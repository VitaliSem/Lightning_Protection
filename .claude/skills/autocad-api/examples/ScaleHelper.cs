// LP.Plugin/Acad/ScaleHelper.cs — logic from annotation-scales-guide.md.
// C# 12, nullable, implicit usings; compiles for net48 (AutoCAD 2024) and net8.0 (AutoCAD 2025+).
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using AcException = Autodesk.AutoCAD.Runtime.Exception;

namespace LP.Plugin.Acad;

/// <summary>Annotation scales of Model space (CANNOSCALE). Make sure db.TileMode == true.</summary>
public static class ScaleHelper
{
    private const string AnnoScalesKey = "ACDB_ANNOTATIONSCALES";

    /// <summary>All scales of the drawing (object contexts — no transaction needed).</summary>
    public static ObjectContextCollection GetScaleCollection(Database db) =>
        db.ObjectContextManager.GetContextCollection(AnnoScalesKey);

    // ---------- 1. Current ----------

    public static AnnotationScale GetCurrentScale(Database db) => db.Cannoscale;

    public static string DescribeCurrentScale(Database db)
    {
        AnnotationScale s = db.Cannoscale;
        return string.Format(CultureInfo.InvariantCulture,
            "Current scale: {0} (paper {1} : drawing {2}, factor {3})", s.Name, s.PaperUnits, s.DrawingUnits, s.Scale);
    }

    // ---------- 2. Find ----------

    public static bool ScaleExists(Database db, string name) => GetScaleCollection(db).HasContext(name);

    public static AnnotationScale? FindScale(Database db, string name)
    {
        ObjectContextCollection occ = GetScaleCollection(db);
        return occ.HasContext(name) ? occ.GetContext(name) as AnnotationScale : null;
    }

    public static List<AnnotationScale> GetAllScales(Database db)
    {
        var result = new List<AnnotationScale>();
        foreach (ObjectContext ctx in GetScaleCollection(db))
        {
            if (ctx is AnnotationScale s)
                result.Add(s);
        }
        return result;
    }

    /// <summary>Finds a scale by its paper : drawing ratio regardless of the name ("1:50" vs "1/50").</summary>
    public static AnnotationScale? FindScaleByRatio(Database db, double paperUnits, double drawingUnits, double tolerance = 1e-9)
    {
        double wanted = paperUnits / drawingUnits;
        return GetAllScales(db).FirstOrDefault(s => Math.Abs(s.PaperUnits / s.DrawingUnits - wanted) < tolerance);
    }

    // ---------- 3. Create ----------

    public static string FormatScaleName(double paperUnits, double drawingUnits) =>
        string.Format(CultureInfo.InvariantCulture, "{0:0.######}:{1:0.######}", paperUnits, drawingUnits);

    /// <summary>Adds a scale to the list. False if the name is taken. Does not make it current.</summary>
    public static bool CreateScale(Database db, string name, double paperUnits, double drawingUnits)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The scale name must not be empty.", nameof(name));
        if (paperUnits <= 0 || drawingUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(paperUnits), "Paper and drawing units must be positive.");

        ObjectContextCollection occ = GetScaleCollection(db);
        if (occ.HasContext(name))
            return false;

        using var scale = new AnnotationScale { Name = name, PaperUnits = paperUnits, DrawingUnits = drawingUnits };
        occ.AddContext(scale);
        return true;
    }

    /// <summary>Name of the scale paper:drawing, created if missing (CANNOSCALE unchanged).</summary>
    public static string EnsureScale(Database db, double paperUnits, double drawingUnits)
    {
        AnnotationScale? existing = FindScaleByRatio(db, paperUnits, drawingUnits);
        if (existing is not null)
            return existing.Name;

        string name = FormatScaleName(paperUnits, drawingUnits);
        CreateScale(db, name, paperUnits, drawingUnits);
        return name;
    }

    // ---------- 4. Set current ----------

    public static bool SetCurrentScale(Database db, string name)
    {
        AnnotationScale? scale = FindScale(db, name);
        if (scale is null)
            return false;

        db.Cannoscale = scale;
        return true;
    }

    /// <summary>Makes paper:drawing current, creating it if missing. Returns the scale name.</summary>
    public static string SetCurrentScale(Database db, double paperUnits, double drawingUnits)
    {
        string name = EnsureScale(db, paperUnits, drawingUnits);
        SetCurrentScale(db, name);
        return name;
    }

    // ---------- 5. Delete ----------

    public static bool RemoveScale(Database db, string name, string fallbackName = "1:1")
    {
        ObjectContextCollection occ = GetScaleCollection(db);
        if (!occ.HasContext(name))
            return false;

        if (string.Equals(db.Cannoscale.Name, name, StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(name, fallbackName, StringComparison.OrdinalIgnoreCase) || !SetCurrentScale(db, fallbackName)))
        {
            return false;                                   // the current scale cannot be removed
        }

        try
        {
            occ.RemoveContext(name);
            return true;
        }
        catch (AcException)
        {
            return false;                                   // AutoCAD refused (e.g. in use)
        }
    }

    // ---------- 6. Switch ----------

    public static string? SwitchScaleBy(Database db, int step)
    {
        List<AnnotationScale> scales = GetAllScales(db);
        if (scales.Count == 0)
            return null;

        string current = db.Cannoscale.Name;
        int index = scales.FindIndex(s => string.Equals(s.Name, current, StringComparison.OrdinalIgnoreCase));
        int next = ((index + step) % scales.Count + scales.Count) % scales.Count;

        SetCurrentScale(db, scales[next].Name);
        return scales[next].Name;
    }

    /// <summary>Temporarily switches CANNOSCALE, runs the action and restores the previous scale.</summary>
    public static void RunAtScale(Database db, string scaleName, Action action)
    {
        string previous = db.Cannoscale.Name;
        if (!SetCurrentScale(db, scaleName))
            throw new InvalidOperationException($"Scale '{scaleName}' not found.");

        try
        {
            action();
        }
        finally
        {
            SetCurrentScale(db, previous);
        }
    }

    // ---------- Scale contexts of annotative objects ----------
    // Public DBObject.AddContext / HasContext / RemoveContext instead of Internal.ObjectContexts.

    /// <summary>Adds an annotation scale to an annotative object (open for write).</summary>
    public static bool AddScaleToObject(Database db, DBObject obj, string scaleName)
    {
        ObjectContextCollection occ = GetScaleCollection(db);
        if (!occ.HasContext(scaleName))
            return false;

        ObjectContext ctx = occ.GetContext(scaleName);
        if (!obj.HasContext(ctx))
            obj.AddContext(ctx);
        return true;
    }

    public static List<string> GetObjectScales(Database db, DBObject obj)
    {
        var result = new List<string>();
        foreach (ObjectContext ctx in GetScaleCollection(db))
        {
            if (obj.HasContext(ctx))
                result.Add(ctx.Name);
        }
        return result;
    }

    /// <summary>Removes a scale from an object; refuses to remove the last one.</summary>
    public static bool RemoveScaleFromObject(Database db, DBObject obj, string scaleName)
    {
        ObjectContextCollection occ = GetScaleCollection(db);
        if (!occ.HasContext(scaleName))
            return false;

        ObjectContext ctx = occ.GetContext(scaleName);
        if (!obj.HasContext(ctx) || GetObjectScales(db, obj).Count <= 1)
            return false;

        obj.RemoveContext(ctx);
        return true;
    }

    /// <summary>Leaves exactly one scale: adds 'scaleName' first (never zero scales), then removes the others.</summary>
    public static bool SetSingleObjectScale(Database db, DBObject obj, string scaleName)
    {
        if (!AddScaleToObject(db, obj, scaleName))
            return false;

        foreach (string name in GetObjectScales(db, obj))
        {
            if (!string.Equals(name, scaleName, StringComparison.OrdinalIgnoreCase))
                RemoveScaleFromObject(db, obj, name);
        }
        return true;
    }
}
