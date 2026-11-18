// LP.Plugin/Acad/PrimitivesHelper.cs — logic from primitives-guide.md.
// C# 12, nullable, implicit usings; compiles for net48 (AutoCAD 2024) and net8.0 (AutoCAD 2025+).
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;                 // ErrorStatus
using AcException = Autodesk.AutoCAD.Runtime.Exception;

namespace LP.Plugin.Acad;

public static class PrimitivesHelper
{
    // ---------- Shared ----------

    /// <summary>Appends an entity to model space and registers it in the transaction.</summary>
    public static ObjectId AppendToModelSpace(Transaction tr, Database db, Entity ent)
    {
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
        ObjectId id = ms.AppendEntity(ent);
        tr.AddNewlyCreatedDBObject(ent, true);
        return id;
    }

    /// <summary>Appends an entity to the active space (model or current layout).</summary>
    public static ObjectId AppendToCurrentSpace(Transaction tr, Database db, Entity ent)
    {
        var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
        ObjectId id = space.AppendEntity(ent);
        tr.AddNewlyCreatedDBObject(ent, true);
        return id;
    }

    /// <summary>Opens an entity of type T for write and applies an action. False if the type differs.</summary>
    public static bool ModifyEntity<T>(Database db, ObjectId id, Action<T> change) where T : Entity
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        if (tr.GetObject(id, OpenMode.ForWrite) is not T ent)
            return false;

        change(ent);
        tr.Commit();
        return true;
    }

    public static bool EraseEntity(Database db, ObjectId id)
    {
        if (id.IsNull || id.IsErased)
            return false;

        using Transaction tr = db.TransactionManager.StartTransaction();
        ((Entity)tr.GetObject(id, OpenMode.ForWrite)).Erase(true);
        tr.Commit();
        return true;
    }

    public static Point2d ToPoint2d(Point3d p) => new(p.X, p.Y);

    // ---------- 1. Line ----------

    public static ObjectId CreateLine(Database db, Point3d start, Point3d end, string? layer = null)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var line = new Line(start, end);
        if (layer is not null)
            line.Layer = layer;                             // must exist

        ObjectId id = AppendToModelSpace(tr, db, line);
        tr.Commit();
        return id;
    }

    // ---------- 2. Circle ----------

    public static Circle NewCircle(Point3d center, double radius, string? layer = null)
    {
        if (radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be positive.");

        var circle = new Circle(center, Vector3d.ZAxis, radius);    // normal Z = XY plane
        if (layer is not null)
            circle.Layer = layer;
        return circle;
    }

    public static ObjectId CreateCircle(Database db, Point3d center, double radius, string? layer = null)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        ObjectId id = AppendToModelSpace(tr, db, NewCircle(center, radius, layer));
        tr.Commit();
        return id;
    }

    // ---------- 3. Polyline ----------

    /// <summary>Polyline through points; bulges[i] belongs to the segment that STARTS at vertex i.</summary>
    public static Polyline NewPolyline(IList<Point2d> points, IList<double>? bulges = null, bool closed = false)
    {
        if (points is null || points.Count < 2)
            throw new ArgumentException("At least two points are required.", nameof(points));
        if (closed && points.Count < 3)
            throw new ArgumentException("A closed polyline needs at least three points.", nameof(points));

        var pl = new Polyline();
        for (int i = 0; i < points.Count; i++)
        {
            double bulge = bulges is not null && i < bulges.Count ? bulges[i] : 0.0;
            pl.AddVertexAt(i, points[i], bulge, 0, 0);      // (index, point, bulge, startWidth, endWidth)
        }

        pl.Closed = closed;                                 // never repeat the first point to close
        return pl;
    }

    public static ObjectId CreatePolyline(Database db, IList<Point2d> points, bool closed = false, string? layer = null)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        Polyline pl = NewPolyline(points, null, closed);
        if (layer is not null)
            pl.Layer = layer;

        ObjectId id = AppendToModelSpace(tr, db, pl);
        tr.Commit();
        return id;
    }

    public static bool AppendLineSegment(Database db, ObjectId polyId, Point2d endPoint) =>
        ModifyEntity<Polyline>(db, polyId, pl => pl.AddVertexAt(pl.NumberOfVertices, endPoint, 0, 0, 0));

    /// <summary>Appends an arc: the bulge goes on the CURRENT last vertex, then the end vertex is added.</summary>
    public static bool AppendArcSegment(Database db, ObjectId polyId, Point2d endPoint, double bulge) =>
        ModifyEntity<Polyline>(db, polyId, pl =>
        {
            int last = pl.NumberOfVertices - 1;
            pl.SetBulgeAt(last, bulge);
            pl.AddVertexAt(last + 1, endPoint, 0, 0, 0);
        });

    /// <summary>Closes a polyline, removing a duplicated first point at the end if present.</summary>
    public static bool ClosePolyline(Database db, ObjectId polyId, double tolerance = 1e-6) =>
        ModifyEntity<Polyline>(db, polyId, pl =>
        {
            int n = pl.NumberOfVertices;
            if (pl.Closed || n < 3)
                return;

            if (pl.GetPoint2dAt(0).GetDistanceTo(pl.GetPoint2dAt(n - 1)) < tolerance)
                pl.RemoveVertexAt(n - 1);

            pl.Closed = true;
        });

    // ---------- 3.2 Bulge math ----------

    /// <summary>bulge = tan(θ/4); θ = included angle in radians, sign = direction (+ CCW).</summary>
    public static double BulgeFromAngle(double includedAngle) => Math.Tan(includedAngle / 4.0);

    /// <summary>Bulge of the arc start → mid → end; sign determined automatically.</summary>
    public static double BulgeFromThreePoints(Point2d start, Point2d mid, Point2d end)
    {
        Vector2d a = start - mid;
        Vector2d b = end - mid;

        double denom = a.Length * b.Length;
        if (denom < Tolerance.Global.EqualPoint)
            return 0.0;

        double cos = Math.Max(-1.0, Math.Min(1.0, a.DotProduct(b) / denom));   // no Math.Clamp in net48
        double theta = 2.0 * (Math.PI - Math.Acos(cos));

        Vector2d v1 = mid - start;
        Vector2d v2 = end - mid;
        double cross = v1.X * v2.Y - v1.Y * v2.X;

        return (cross >= 0 ? 1.0 : -1.0) * Math.Tan(theta / 4.0);
    }

    /// <summary>
    /// Bulge of an arc from a chord and a radius. Four arcs are possible:
    /// largeArc selects minor/major, counterClockwise the sign. A minor CCW arc bulges to the RIGHT of start → end.
    /// </summary>
    public static double BulgeFromRadius(Point2d start, Point2d end, double radius,
        bool largeArc = false, bool counterClockwise = true)
    {
        if (radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be positive.");

        double chord = start.GetDistanceTo(end);
        if (chord < Tolerance.Global.EqualPoint)
            throw new ArgumentException("The start and end points coincide.");

        double half = chord / (2.0 * radius);               // sin(θ/2)
        if (half > 1.0 + 1e-9)
            throw new ArgumentException($"The radius is too small: it must be at least {chord / 2.0}.");

        double theta = 2.0 * Math.Asin(Math.Min(half, 1.0));
        if (largeArc)
            theta = 2.0 * Math.PI - theta;

        double bulge = Math.Tan(theta / 4.0);
        return counterClockwise ? bulge : -bulge;
    }

    /// <summary>
    /// Bulge of the arc start → end tangent to 'incomingDirection' at 'start': bulge = tan(φ/2),
    /// φ = signed angle from the incoming direction to the chord. Not part of the original guide.
    /// </summary>
    public static double TangentBulge(Vector2d incomingDirection, Point2d start, Point2d end)
    {
        Vector2d chord = end - start;
        double cross = incomingDirection.X * chord.Y - incomingDirection.Y * chord.X;
        double phi = Math.Atan2(cross, incomingDirection.DotProduct(chord));
        return Math.Tan(phi / 2.0);
    }

    /// <summary>Midpoint of the arc start → end with the given bulge (sagitta = bulge · chord / 2, CCW to the right).</summary>
    public static Point2d ArcMidpoint(Point2d start, Point2d end, double bulge)
    {
        Vector2d chord = end - start;
        Point2d mid = start + chord * 0.5;
        if (chord.Length < Tolerance.Global.EqualPoint)
            return mid;

        Vector2d right = new Vector2d(chord.Y, -chord.X).GetNormal();
        return mid + right * (bulge * chord.Length / 2.0);
    }

    // ---------- 4. Common operations ----------

    public static bool Move(Database db, ObjectId id, Vector3d offset) =>
        ModifyEntity<Entity>(db, id, e => e.TransformBy(Matrix3d.Displacement(offset)));

    public static bool Rotate(Database db, ObjectId id, double angle, Point3d basePoint) =>
        ModifyEntity<Entity>(db, id, e => e.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, basePoint)));

    public static bool Scale(Database db, ObjectId id, double factor, Point3d basePoint) =>
        ModifyEntity<Entity>(db, id, e => e.TransformBy(Matrix3d.Scaling(factor, basePoint)));

    public static ObjectId CopyEntity(Database db, ObjectId id, Vector3d offset)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var source = (Entity)tr.GetObject(id, OpenMode.ForRead);
        var copy = (Entity)source.Clone();
        copy.TransformBy(Matrix3d.Displacement(offset));

        ObjectId newId = AppendToModelSpace(tr, db, copy);
        tr.Commit();
        return newId;
    }

    /// <summary>Selects primitives in the current space by DXF names (comma list) and optional layer.</summary>
    public static ObjectId[] SelectPrimitives(Editor ed, string dxfNames = "LINE,CIRCLE,LWPOLYLINE", string? layer = null)
    {
        var filter = new List<TypedValue> { new((int)DxfCode.Start, dxfNames) };
        if (layer is not null)
            filter.Add(new TypedValue((int)DxfCode.LayerName, layer));

        PromptSelectionResult res = ed.SelectAll(new SelectionFilter(filter.ToArray()));
        return res.Status == PromptStatus.OK ? res.Value.GetObjectIds() : [];
    }

    /// <summary>Erases the given entities in one transaction, skipping locked layers.</summary>
    public static int EraseAll(Database db, IEnumerable<ObjectId> ids)
    {
        int count = 0;
        using Transaction tr = db.TransactionManager.StartTransaction();

        foreach (ObjectId id in ids)
        {
            if (id.IsNull || id.IsErased)
                continue;
            try
            {
                ((Entity)tr.GetObject(id, OpenMode.ForWrite)).Erase(true);
                count++;
            }
            catch (AcException ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
            {
                // skip objects on locked layers
            }
        }

        tr.Commit();
        return count;
    }
}
