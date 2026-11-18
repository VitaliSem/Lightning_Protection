// LP.Plugin/Acad/TextHelper.cs — logic from text-guide.md.
// C# 12, nullable, implicit usings; compiles for net48 (AutoCAD 2024) and net8.0 (AutoCAD 2025+).
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;                 // ErrorStatus
using AcException = Autodesk.AutoCAD.Runtime.Exception;
// FontDescriptor lives in GraphicsInterface; alias it (a full using would make Polyline etc. ambiguous)
using FontDescriptor = Autodesk.AutoCAD.GraphicsInterface.FontDescriptor;

namespace LP.Plugin.Acad;

public static class TextHelper
{
    // ---------- 1. Single-line text (DBText) ----------

    /// <summary>New DBText (not yet in the database). Append it inside a transaction.</summary>
    public static DBText NewText(Database db, string text, Point3d position, double height,
        double rotation = 0.0, string? layer = null, ObjectId textStyleId = default)
    {
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Text height must be positive.");

        var t = new DBText();
        t.SetDatabaseDefaults(db);                          // current style, layer, color
        t.Position = position;                              // left baseline by default
        t.Height = height;
        t.TextString = text;                                // no line breaks in DBText
        t.Rotation = rotation;                              // radians

        if (!textStyleId.IsNull)
            t.TextStyleId = textStyleId;
        if (layer is not null)
            t.Layer = layer;
        return t;
    }

    public static ObjectId CreateText(Database db, string text, Point3d position, double height,
        double rotation = 0.0, string? layer = null, ObjectId textStyleId = default)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        ObjectId id = PrimitivesHelper.AppendToModelSpace(tr, db,
            NewText(db, text, position, height, rotation, layer, textStyleId));
        tr.Commit();
        return id;
    }

    /// <summary>Non-default justification: set AlignmentPoint and call AdjustAlignment (Position is recalculated).</summary>
    public static void Justify(Database db, DBText t, TextHorizontalMode h, TextVerticalMode v, Point3d alignmentPoint)
    {
        t.HorizontalMode = h;
        t.VerticalMode = v;
        t.AlignmentPoint = alignmentPoint;
        t.AdjustAlignment(db);
    }

    /// <summary>Fit (height kept) or Align (height changes) between two baseline points.</summary>
    public static void FitBetween(Database db, DBText t, Point3d start, Point3d end, bool keepHeight = true)
    {
        t.HorizontalMode = keepHeight ? TextHorizontalMode.TextFit : TextHorizontalMode.TextAlign;
        t.Position = start;
        t.AlignmentPoint = end;
        t.AdjustAlignment(db);
    }

    // ---------- 2. Multiline text (MText) ----------

    /// <summary>New MText. width = wrapping width (0 = no wrapping). Line break in contents: "\\P".</summary>
    public static MText NewMText(Database db, string contents, Point3d location, double textHeight, double width,
        AttachmentPoint attachment = AttachmentPoint.TopLeft, string? layer = null, ObjectId textStyleId = default)
    {
        if (textHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(textHeight), "Text height must be positive.");

        var mt = new MText();
        mt.SetDatabaseDefaults(db);
        mt.Contents = contents;
        mt.Location = location;
        mt.TextHeight = textHeight;
        mt.Width = width;
        mt.Attachment = attachment;                         // which box point sits at Location

        if (!textStyleId.IsNull)
            mt.TextStyleId = textStyleId;
        if (layer is not null)
            mt.Layer = layer;
        return mt;
    }

    public static ObjectId CreateMText(Database db, string contents, Point3d location, double textHeight, double width,
        AttachmentPoint attachment = AttachmentPoint.TopLeft, string? layer = null)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        ObjectId id = PrimitivesHelper.AppendToModelSpace(tr, db,
            NewMText(db, contents, location, textHeight, width, attachment, layer));
        tr.Commit();
        return id;
    }

    /// <summary>
    /// Annotative MText inside the caller's transaction. paperHeight = height on the sheet (mm).
    /// The scale must exist (ScaleHelper.EnsureScale). Most predictable when CANNOSCALE == scaleName
    /// (AutoCAD attaches the current scale when Annotative is switched on; SetSingleObjectScale then
    /// leaves exactly 'scaleName').
    /// </summary>
    public static ObjectId CreateAnnotativeMText(Transaction tr, Database db, string contents, Point3d location,
        double paperHeight, string scaleName, AttachmentPoint attachment = AttachmentPoint.MiddleCenter,
        string? layer = null, bool backgroundMask = true)
    {
        MText mt = NewMText(db, contents, location, paperHeight, 0.0, attachment, layer);
        ObjectId id = PrimitivesHelper.AppendToModelSpace(tr, db, mt);

        mt.Annotative = AnnotativeStates.True;              // set once the object is DB-resident
        ScaleHelper.SetSingleObjectScale(db, mt, scaleName);

        if (backgroundMask)
        {
            mt.BackgroundFill = true;
            mt.UseBackgroundColor = true;                   // drawing background colour
            mt.BackgroundScaleFactor = 1.2;
        }
        return id;
    }

    /// <summary>Sets the text box width equal to the natural width of the text.</summary>
    public static bool FitMTextWidthToContent(Database db, ObjectId mtextId) =>
        PrimitivesHelper.ModifyEntity<MText>(db, mtextId, mt =>
        {
            mt.Width = 0.0;
            double natural = mt.ActualWidth;                // may be stale before commit
            if (natural > 0)
                mt.Width = natural;
        });

    public static double GetTextWidth(Database db, ObjectId id)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        Extents3d ext = ((Entity)tr.GetObject(id, OpenMode.ForRead)).GeometricExtents;
        return ext.MaxPoint.X - ext.MinPoint.X;             // along X; rotated text needs its own frame
    }

    // ---------- 4. Text styles ----------

    public static ObjectId GetTextStyleId(Database db, string styleName)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
        return tst.Has(styleName) ? tst[styleName] : ObjectId.Null;
    }

    /// <summary>
    /// Creates (or returns) a text style. typeface = TrueType name ("Arial") or an .shx file ("simplex.shx").
    /// textSize = 0 keeps the height variable (recommended).
    /// </summary>
    public static ObjectId EnsureTextStyle(Transaction tr, Database db, string styleName, string typeface,
        double textSize = 0.0, bool bold = false, bool italic = false, bool annotative = false, double widthFactor = 1.0)
    {
        SymbolUtilityServices.ValidateSymbolName(styleName, false);

        var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
        if (tst.Has(styleName))
            return tst[styleName];

        var tsr = new TextStyleTableRecord { Name = styleName, TextSize = textSize, XScale = widthFactor };

        if (typeface.EndsWith(".shx", StringComparison.OrdinalIgnoreCase))
            tsr.FileName = typeface;
        else
            tsr.Font = new FontDescriptor(typeface, bold, italic, 0, 0);

        if (annotative)
            tsr.Annotative = AnnotativeStates.True;

        tst.UpgradeOpen();
        ObjectId id = tst.Add(tsr);
        tr.AddNewlyCreatedDBObject(tsr, true);
        return id;
    }

    /// <summary>Font/orientation changes affect all text of the style; call ed.Regen() afterwards.</summary>
    public static bool ModifyTextStyle(Database db, string styleName, Action<TextStyleTableRecord> change)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
        if (!tst.Has(styleName))
            return false;

        change((TextStyleTableRecord)tr.GetObject(tst[styleName], OpenMode.ForWrite));
        tr.Commit();
        return true;
    }

    // ---------- 5. Color and layer ----------

    public static bool SetColor(Database db, ObjectId id, short aci) =>
        PrimitivesHelper.ModifyEntity<Entity>(db, id, e => e.ColorIndex = aci);

    public static bool SetColorByLayer(Database db, ObjectId id) =>
        PrimitivesHelper.ModifyEntity<Entity>(db, id, e => e.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256));

    // ---------- 6. Bulk ----------

    public static ObjectId[] SelectAllText(Editor ed, string? layer = null)
    {
        var filter = new List<TypedValue> { new((int)DxfCode.Start, "TEXT,MTEXT") };
        if (layer is not null)
            filter.Add(new TypedValue((int)DxfCode.LayerName, layer));

        PromptSelectionResult res = ed.SelectAll(new SelectionFilter(filter.ToArray()));
        return res.Status == PromptStatus.OK ? res.Value.GetObjectIds() : [];
    }

    /// <summary>Applies an action to every DBText/MText in the list (one transaction, locked layers skipped).</summary>
    public static int ForEachText(Database db, IEnumerable<ObjectId> ids, Action<Entity> change)
    {
        int count = 0;
        using Transaction tr = db.TransactionManager.StartTransaction();

        foreach (ObjectId id in ids)
        {
            try
            {
                var ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                if (ent is DBText or MText)
                {
                    change(ent);
                    count++;
                }
            }
            catch (AcException ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
            {
                // skip
            }
        }

        tr.Commit();
        return count;
    }

    /// <summary>Same height for DBText (Height) and MText (TextHeight; inline \H codes keep their own).</summary>
    public static void SetHeight(Entity e, double height)
    {
        switch (e)
        {
            case DBText t: t.Height = height; break;
            case MText mt: mt.TextHeight = height; break;
        }
    }

    /// <summary>Case-sensitive replace in model space. MText is searched in Contents (with codes).</summary>
    public static int ReplaceText(Database db, string find, string replace)
    {
        int count = 0;
        using Transaction tr = db.TransactionManager.StartTransaction();
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);

        foreach (ObjectId id in ms)
        {
            try
            {
                switch (tr.GetObject(id, OpenMode.ForRead))
                {
                    case DBText t when t.TextString.Contains(find):
                        t.UpgradeOpen();
                        t.TextString = t.TextString.Replace(find, replace);
                        count++;
                        break;
                    case MText mt when mt.Contents.Contains(find):
                        mt.UpgradeOpen();
                        mt.Contents = mt.Contents.Replace(find, replace);
                        count++;
                        break;
                }
            }
            catch (AcException ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
            {
                // skip
            }
        }

        tr.Commit();
        return count;
    }

    /// <summary>Replaces a DBText with an equivalent MText (content, height, rotation, style, layer, color).</summary>
    public static ObjectId ConvertTextToMText(Database db, ObjectId textId)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var src = (DBText)tr.GetObject(textId, OpenMode.ForWrite);

        var mt = new MText();
        mt.SetDatabaseDefaults(db);
        mt.Contents = src.TextString;
        mt.Location = src.Position;
        mt.TextHeight = src.Height;
        mt.Rotation = src.Rotation;
        mt.TextStyleId = src.TextStyleId;
        mt.Layer = src.Layer;
        mt.Color = src.Color;
        mt.Attachment = AttachmentPoint.BottomLeft;         // approximates DBText baseline-left
        mt.Width = 0.0;

        ObjectId newId = PrimitivesHelper.AppendToModelSpace(tr, db, mt);
        src.Erase(true);
        tr.Commit();
        return newId;
    }
}
