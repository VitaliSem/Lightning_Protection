// LP.Plugin/Acad/LayerHelper.cs — logic from layers-general-guide.md.
// C# 12, nullable, implicit usings; compiles for net48 (AutoCAD 2024) and net8.0 (AutoCAD 2025+).
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AcException = Autodesk.AutoCAD.Runtime.Exception;

namespace LP.Plugin.Acad;

/// <summary>Project layer names. Keep all LP layers here.</summary>
public static class LpLayers
{
    public const string Zone = "LP-ZONE";           // protection zone outlines
    public const string Protector = "LP-PROTECTOR"; // rod / wire symbols
    public const string Annotation = "LP-ANNO";     // labels
    public const string Auxiliary = "LP-AUX";       // construction geometry, not plotted
}

public static class LayerHelper
{
    // ---------- 1. Find ----------

    public static bool LayerExists(Database db, string layerName)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        return lt.Has(layerName);
    }

    /// <summary>Layer id or ObjectId.Null. lt[name] throws eKeyNotFound, so Has() first.</summary>
    public static ObjectId GetLayerId(Transaction tr, Database db, string layerName)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        return lt.Has(layerName) ? lt[layerName] : ObjectId.Null;
    }

    public static ObjectId GetLayerId(Database db, string layerName)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        return GetLayerId(tr, db, layerName);
    }

    /// <summary>Names of layers matching a condition, e.g. l => l.IsFrozen.</summary>
    public static List<string> FindLayers(Database db, Func<LayerTableRecord, bool>? predicate = null)
    {
        var result = new List<string>();
        using Transaction tr = db.TransactionManager.StartTransaction();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

        foreach (ObjectId id in lt)                         // erased records are skipped
        {
            var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
            if (predicate is null || predicate(ltr))
                result.Add(ltr.Name);
        }
        return result;
    }

    // ---------- 2. Create ----------

    /// <summary>Returns the id of an existing layer or creates it inside the caller's transaction.</summary>
    public static ObjectId EnsureLayer(
        Transaction tr, Database db, string layerName, short colorIndex = 7,
        bool isPlottable = true, string? linetype = null, string? description = null)
    {
        SymbolUtilityServices.ValidateSymbolName(layerName, false);

        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(layerName))
            return lt[layerName];

        var ltr = new LayerTableRecord
        {
            Name = layerName,
            Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex),
            IsPlottable = isPlottable,
        };

        if (description is not null)
            ltr.Description = description;

        if (linetype is not null)
        {
            var ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
            if (ltt.Has(linetype))                          // linetype must already be loaded
                ltr.LinetypeObjectId = ltt[linetype];
        }

        lt.UpgradeOpen();
        ObjectId id = lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
        return id;
    }

    public static ObjectId EnsureLayer(Database db, string layerName, short colorIndex = 7, bool isPlottable = true)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        ObjectId id = EnsureLayer(tr, db, layerName, colorIndex, isPlottable);
        tr.Commit();
        return id;
    }

    /// <summary>Creates all LP layers used by the plugin.</summary>
    public static void EnsureProjectLayers(Transaction tr, Database db)
    {
        EnsureLayer(tr, db, LpLayers.Zone, 1, description: "Lightning protection zones");
        EnsureLayer(tr, db, LpLayers.Protector, 5, description: "Lightning protectors");
        EnsureLayer(tr, db, LpLayers.Annotation, 7, description: "Lightning protection labels");
        EnsureLayer(tr, db, LpLayers.Auxiliary, 8, isPlottable: false, description: "Construction geometry");
    }

    // ---------- 3. Delete ----------

    public static bool DeleteLayer(Database db, string layerName, out string? reason)
    {
        reason = null;

        if (layerName == "0" || layerName.Equals("Defpoints", StringComparison.OrdinalIgnoreCase))
        {
            reason = "A system layer cannot be deleted.";
            return false;
        }

        using Transaction tr = db.TransactionManager.StartTransaction();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

        if (!lt.Has(layerName))
        {
            reason = "Layer not found.";
            return false;
        }

        ObjectId layerId = lt[layerName];
        if (layerId == db.Clayer)
        {
            reason = "The current layer cannot be deleted.";
            return false;
        }

        // Purge removes from the collection every id that cannot be deleted
        var ids = new ObjectIdCollection { layerId };
        db.Purge(ids);

        if (ids.Count == 0)
        {
            reason = "The layer contains objects or is referenced (blocks, xrefs, etc.).";
            return false;
        }

        try
        {
            var ltr = (LayerTableRecord)tr.GetObject(ids[0], OpenMode.ForWrite);
            ltr.Erase(true);
            tr.Commit();
            return true;
        }
        catch (AcException ex)
        {
            reason = "AutoCAD error: " + ex.Message;
            return false;
        }
    }

    /// <summary>Erases every entity on the layer (also on locked layers), then the layer. Irreversible.</summary>
    public static bool DeleteLayerWithObjects(Database db, string layerName, out string? reason)
    {
        List<ObjectId> entityIds = GetEntitiesOnLayer(db, layerName);

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            foreach (ObjectId entId in entityIds)
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForWrite, false, true); // forceOpenOnLockedLayer
                ent.Erase(true);
            }
            tr.Commit();
        }

        return DeleteLayer(db, layerName, out reason);
    }

    // ---------- 4. Current layer ----------

    /// <summary>Makes a layer current; a frozen layer is thawed first when thawIfFrozen is true.</summary>
    public static bool SetCurrentLayer(Database db, string layerName, bool thawIfFrozen = true)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (!lt.Has(layerName))
            return false;

        var ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForRead);
        if (ltr.IsFrozen)
        {
            if (!thawIfFrozen)
                return false;                               // a frozen layer cannot be current
            ltr.UpgradeOpen();
            ltr.IsFrozen = false;
        }

        db.Clayer = ltr.ObjectId;
        tr.Commit();
        return true;
    }

    // ---------- 5. Objects on a layer ----------

    /// <summary>Fast: current space only, skips frozen layers; wildcards allowed ("LP-*").</summary>
    public static ObjectId[] SelectByLayer(Editor ed, string layerPattern, string? dxfTypes = null)
    {
        var filter = new List<TypedValue>();
        if (dxfTypes is not null)
            filter.Add(new TypedValue((int)DxfCode.Start, dxfTypes));
        filter.Add(new TypedValue((int)DxfCode.LayerName, layerPattern));

        PromptSelectionResult res = ed.SelectAll(new SelectionFilter(filter.ToArray()));
        return res.Status == PromptStatus.OK ? res.Value.GetObjectIds() : [];
    }

    /// <summary>Reliable: all layouts (and optionally block definitions), independent of layer state.</summary>
    public static List<ObjectId> GetEntitiesOnLayer(Database db, string layerName, bool layoutsOnly = true)
    {
        var result = new List<ObjectId>();
        using Transaction tr = db.TransactionManager.StartTransaction();
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

        foreach (ObjectId btrId in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            if (layoutsOnly && !btr.IsLayout)
                continue;

            foreach (ObjectId entId in btr)
            {
                if (tr.GetObject(entId, OpenMode.ForRead) is Entity ent &&
                    string.Equals(ent.Layer, layerName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(entId);
                }
            }
        }
        return result;
    }

    // ---------- 6. State ----------

    private static bool ModifyLayer(Database db, string layerName, Action<LayerTableRecord> change)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (!lt.Has(layerName))
            return false;

        var ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
        change(ltr);
        tr.Commit();
        return true;
    }

    /// <summary>The current layer cannot be frozen. Call ed.Regen() afterwards.</summary>
    public static bool SetFrozen(Database db, string layerName, bool frozen) =>
        !(frozen && GetLayerId(db, layerName) == db.Clayer) &&
        ModifyLayer(db, layerName, l => l.IsFrozen = frozen);

    public static bool SetPlottable(Database db, string layerName, bool plottable) =>
        !layerName.Equals("Defpoints", StringComparison.OrdinalIgnoreCase) &&   // never plotted anyway
        ModifyLayer(db, layerName, l => l.IsPlottable = plottable);

    public static bool SetLocked(Database db, string layerName, bool locked) =>
        ModifyLayer(db, layerName, l => l.IsLocked = locked);

    public static bool SetOff(Database db, string layerName, bool off) =>
        ModifyLayer(db, layerName, l => l.IsOff = off);

    public static string GetLayerState(Database db, string layerName)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (!lt.Has(layerName))
            return "layer not found";

        var l = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForRead);
        return $"{l.Name}: frozen={l.IsFrozen}, plottable={l.IsPlottable}, locked={l.IsLocked}, off={l.IsOff}";
    }
}
