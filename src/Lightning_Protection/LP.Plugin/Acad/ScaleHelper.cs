using Autodesk.AutoCAD.DatabaseServices;
using LP.Plugin.Constants;

namespace LP.Plugin.Acad;

/// <summary>Annotation scales (CANNOSCALE) and scale contexts of annotative objects.</summary>
public static class ScaleHelper
{
    public static ObjectContextCollection GetScaleCollection(Database db) =>
        db.ObjectContextManager.GetContextCollection(AcadConstants.AnnotationScalesKey);

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

    /// <summary>Removes an annotation scale from an object unless it is the object's only scale.</summary>
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

    /// <summary>Returns the name of the drawing's 1:1 annotation scale, creating the scale when missing.</summary>
    public static string EnsureOneToOneScale(Database db)
    {
        ObjectContextCollection occ = GetScaleCollection(db);

        foreach (ObjectContext ctx in occ)
        {
            if (ctx is AnnotationScale scale && Math.Abs(scale.PaperUnits - scale.DrawingUnits) < 1e-9)
                return scale.Name;
        }

        using var oneToOne = new AnnotationScale();

        oneToOne.Name = AcadConstants.OneToOneScaleName;
        oneToOne.PaperUnits = 1.0;
        oneToOne.DrawingUnits = 1.0;

        occ.AddContext(oneToOne);

        return AcadConstants.OneToOneScaleName;
    }

    /// <summary>Runs an action with the given annotation scale temporarily made current.</summary>
    public static void RunAtScale(Database db, string scaleName, Action action)
    {
        AnnotationScale previous = db.Cannoscale;
        db.Cannoscale = (AnnotationScale)GetScaleCollection(db).GetContext(scaleName);

        try
        {
            action();
        }
        finally
        {
            db.Cannoscale = previous;
        }
    }

    /// <summary>Makes the given annotation scale the only scale of an object.</summary>
    public static bool SetSingleObjectScale(Database db, DBObject obj, string scaleName)
    {
        if (!AddScaleToObject(db, obj, scaleName))
            return false;

        foreach (var name in GetObjectScales(db, obj))
        {
            if (!string.Equals(name, scaleName, StringComparison.OrdinalIgnoreCase))
                RemoveScaleFromObject(db, obj, name);
        }

        return true;
    }
}
