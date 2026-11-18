// LP.Plugin/Drawing/ProtectionAreaDrawer.cs — draws an LP.Core ProtectionArea into AutoCAD.
// C# 12, nullable, implicit usings; compiles for net48 and net8.0. Requires a ProjectReference to LP.Core.
//
// Interpretation rules (see references/protection-areas.md):
//   1. every point has a Radius and Count < 3  -> each point is a circle centre (single rod, double rod not combined)
//   2. otherwise                                -> one Polyline, closed when the last point has IsLastPoint
//   3. Radius == null -> straight segment starts at the vertex; Radius set -> arc of that radius starts at the vertex
//   4. arc variant: tangent to a preceding straight segment when possible, otherwise the arc that bulges away from
//      the centroid of the vertices (the model stores no arc centre or direction).
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using LP.Core.Models.Base;
using LP.Plugin.Acad;

namespace LP.Plugin.Drawing;

public static class ProtectionAreaDrawer
{
    private const double ParallelTolerance = 1e-9;

    /// <summary>Converts a ProtectionArea into new (not yet appended) entities: circles or one polyline.</summary>
    public static List<Entity> ToEntities(ProtectionArea area)
    {
        if (area?.Points is not { Count: > 0 } pts)
            throw new ArgumentException("Protection area has no points.", nameof(area));

        if (pts.Count < 3 && pts.All(p => p.Radius.HasValue))
            return pts.Select(ToCircle).ToList();

        return [ToPolyline(pts)];
    }

    /// <summary>Converts and appends to model space on the given layer (inside the caller's transaction).</summary>
    public static IReadOnlyList<ObjectId> Draw(Transaction tr, Database db, ProtectionArea area, string? layer)
    {
        var ids = new List<ObjectId>();
        foreach (Entity ent in ToEntities(area))
        {
            ent.SetDatabaseDefaults(db);
            if (layer is not null)
                ent.Layer = layer;                          // must exist (LayerHelper.EnsureProjectLayers)
            ids.Add(PrimitivesHelper.AppendToModelSpace(tr, db, ent));
        }
        return ids;
    }

    private static Entity ToCircle(ProtectionPoint p)
    {
        double r = p.Radius ?? 0;
        if (r <= 0)
            throw new ArgumentException("A circular protection area needs a positive radius.");
        return new Circle(new Point3d(p.Point.X, p.Point.Y, 0), Vector3d.ZAxis, r);
    }

    private static Polyline ToPolyline(List<ProtectionPoint> pts)
    {
        int n = pts.Count;
        bool closed = pts[n - 1].IsLastPoint;
        int segmentCount = closed ? n : n - 1;
        Point2d centroid = Centroid(pts);

        var pl = new Polyline();
        for (int i = 0; i < n; i++)
            pl.AddVertexAt(i, ToPoint2d(pts[i].Point), 0, 0, 0);    // Z ignored in plan view

        for (int i = 0; i < segmentCount; i++)
        {
            if (pts[i].Radius is not double radius)
                continue;

            Point2d start = ToPoint2d(pts[i].Point);
            Point2d end = ToPoint2d(pts[(i + 1) % n].Point);
            pl.SetBulgeAt(i, ArcBulge(pts, i, start, end, radius, closed, centroid));
        }

        pl.Closed = closed;                                 // never by repeating the first vertex
        return pl;
    }

    private static double ArcBulge(List<ProtectionPoint> pts, int i, Point2d start, Point2d end,
        double radius, bool closed, Point2d centroid)
    {
        int n = pts.Count;
        int prev = (i - 1 + n) % n;
        bool hasStraightPrevious = (i > 0 || closed) && !pts[prev].Radius.HasValue;

        // a) tangent to the previous straight segment (exact for external-tangent outlines)
        if (hasStraightPrevious)
        {
            Vector2d incoming = start - ToPoint2d(pts[prev].Point);
            Vector2d chord = end - start;
            if (incoming.Length > Tolerance.Global.EqualPoint)
            {
                double cross = incoming.X * chord.Y - incoming.Y * chord.X;
                double phi = Math.Atan2(cross, incoming.DotProduct(chord));

                if (Math.Abs(Math.Sin(phi)) > ParallelTolerance)        // not collinear with the chord
                {
                    bool largeArc = Math.Abs(phi) > Math.PI / 2.0;
                    return PrimitivesHelper.BulgeFromRadius(start, end, radius, largeArc, counterClockwise: phi > 0);
                }
            }
        }

        // b) minor arc (semicircle if chord = 2R) bulging away from the centroid
        double ccw = PrimitivesHelper.BulgeFromRadius(start, end, radius, largeArc: false, counterClockwise: true);
        double dCcw = PrimitivesHelper.ArcMidpoint(start, end, ccw).GetDistanceTo(centroid);
        double dCw = PrimitivesHelper.ArcMidpoint(start, end, -ccw).GetDistanceTo(centroid);
        return dCcw >= dCw ? ccw : -ccw;
    }

    private static Point2d Centroid(List<ProtectionPoint> pts) =>
        new(pts.Average(p => p.Point.X), pts.Average(p => p.Point.Y));

    private static Point2d ToPoint2d(Point3D p) => new(p.X, p.Y);
}
