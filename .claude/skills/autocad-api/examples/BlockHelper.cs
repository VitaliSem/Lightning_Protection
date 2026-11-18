// LP.Plugin/Acad/BlockHelper.cs — logic from blocks-guide.md.
// C# 12, nullable, implicit usings; compiles for net48 (AutoCAD 2024) and net8.0 (AutoCAD 2025+).
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;                 // RXClass, RXObject, ErrorStatus
using AcException = Autodesk.AutoCAD.Runtime.Exception;

namespace LP.Plugin.Acad;

public static class BlockHelper
{
    // ---------- 1. Exists ----------

    public static ObjectId GetBlockId(Transaction tr, Database db, string blockName)
    {
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        return bt.Has(blockName) ? bt[blockName] : ObjectId.Null;
    }

    public static bool BlockExists(Database db, string blockName)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        return !GetBlockId(tr, db, blockName).IsNull;
    }

    /// <summary>User-defined blocks only (no layouts, optionally no anonymous / xref blocks).</summary>
    public static List<string> GetBlockNames(Database db, bool includeAnonymous = false, bool includeXrefs = false)
    {
        var names = new List<string>();
        using Transaction tr = db.TransactionManager.StartTransaction();
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

        foreach (ObjectId id in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
            if (btr.IsLayout) continue;
            if (!includeAnonymous && btr.IsAnonymous) continue;
            if (!includeXrefs && (btr.IsFromExternalReference || btr.IsFromOverlayReference)) continue;
            names.Add(btr.Name);
        }
        return names;
    }

    /// <summary>Uses the block if present, otherwise imports a whole DWG as its definition.</summary>
    public static ObjectId EnsureBlockFromFile(Database db, string blockName, string dwgPath)
    {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            ObjectId id = GetBlockId(tr, db, blockName);
            if (!id.IsNull)
                return id;
        }

        using var source = new Database(false, true);
        source.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");
        return db.Insert(blockName, source, true);          // re-inserting redefines an existing block
    }

    // ---------- 2. Create a definition ----------

    /// <summary>
    /// Creates a block from NEW entities (not yet in the DB). Returns the existing id if the name is taken
    /// (the entities are then disposed). Put geometry on layer "0" with ColorIndex 0 (ByBlock) for adaptable symbols.
    /// </summary>
    public static ObjectId EnsureBlock(Transaction tr, Database db, string blockName, Point3d origin,
        IEnumerable<Entity> entities, string? description = null, bool annotative = false)
    {
        SymbolUtilityServices.ValidateSymbolName(blockName, false);

        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        if (bt.Has(blockName))
        {
            foreach (Entity e in entities)
                e.Dispose();                                // never added to the DB
            return bt[blockName];
        }

        var btr = new BlockTableRecord { Name = blockName, Origin = origin };   // Origin = base point
        if (description is not null)
            btr.Comments = description;
        if (annotative)
        {
            btr.BlockScaling = BlockScaling.Uniform;        // required for annotative blocks
            btr.Annotative = AnnotativeStates.True;
        }

        bt.UpgradeOpen();
        ObjectId btrId = bt.Add(btr);
        tr.AddNewlyCreatedDBObject(btr, true);

        foreach (Entity ent in entities)
        {
            btr.AppendEntity(ent);
            tr.AddNewlyCreatedDBObject(ent, true);

            // Attributes of an annotative block must be annotative too (set once DB-resident)
            if (annotative && ent is AttributeDefinition)
                ent.Annotative = AnnotativeStates.True;
        }
        return btrId;
    }

    /// <summary>Attribute definition for a block being built. Tag: no spaces or '!', stored upper-case.</summary>
    public static AttributeDefinition NewAttributeDefinition(Database db, string tag, string prompt, string defaultValue,
        Point3d position, double height, bool invisible = false, bool constant = false, bool centered = true)
    {
        if (string.IsNullOrWhiteSpace(tag) || tag.Contains(" ") || tag.Contains("!"))
            throw new ArgumentException("The tag must not be empty or contain spaces or '!'.", nameof(tag));

        var ad = new AttributeDefinition();
        ad.SetDatabaseDefaults(db);
        ad.Tag = tag.ToUpperInvariant();
        ad.Prompt = prompt;
        ad.TextString = defaultValue;
        ad.Position = position;                             // block coordinates
        ad.Height = height;
        ad.Invisible = invisible;
        ad.Constant = constant;
        ad.LockPositionInBlock = true;

        if (centered)
        {
            ad.Justify = AttachmentPoint.MiddleCenter;
            ad.AlignmentPoint = position;
            ad.AdjustAlignment(db);
        }
        return ad;
    }

    // ---------- 3. Edit a definition ----------

    /// <summary>Opens a definition for write, applies the edit and refreshes all its references.</summary>
    public static bool ModifyBlockDefinition(Database db, string blockName, Action<Transaction, BlockTableRecord> edit)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        ObjectId id = GetBlockId(tr, db, blockName);
        if (id.IsNull)
            return false;

        var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForWrite);
        edit(tr, btr);

        foreach (ObjectId refId in btr.GetBlockReferenceIds(false, true))
            ((BlockReference)tr.GetObject(refId, OpenMode.ForWrite, false, true)).RecordGraphicsModified(true);

        tr.Commit();
        return true;
    }

    // ---------- 4. Delete a definition ----------

    public static bool DeleteBlock(Database db, string blockName, bool eraseReferences, out string? reason)
    {
        reason = null;
        using Transaction tr = db.TransactionManager.StartTransaction();

        ObjectId btrId = GetBlockId(tr, db, blockName);
        if (btrId.IsNull) { reason = "Block not found."; return false; }

        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
        if (btr.IsLayout) { reason = "Model space and layouts cannot be deleted."; return false; }
        if (btr.IsFromExternalReference || btr.IsFromOverlayReference)
        {
            reason = "This is an external reference - detach it instead.";
            return false;
        }

        ObjectIdCollection refs = btr.GetBlockReferenceIds(true, false);
        if (refs.Count > 0 && !eraseReferences)
        {
            reason = $"The block has {refs.Count} reference(s).";
            return false;
        }

        foreach (ObjectId refId in refs)
            ((BlockReference)tr.GetObject(refId, OpenMode.ForWrite, false, true)).Erase(true);

        var ids = new ObjectIdCollection { btrId };
        db.Purge(ids);

        if (ids.Count == 0)
        {
            reason = "The block is still in use (for example nested in another block).";
            return false;                                   // not committed -> erased references come back
        }

        btr.UpgradeOpen();
        btr.Erase(true);
        tr.Commit();
        return true;
    }

    // ---------- 5. Insert ----------

    /// <summary>
    /// Inserts a reference into 'spaceId' (model space if Null) and creates its attribute references,
    /// inside the caller's transaction. For annotative blocks pass 'annotationScale': it is attached BEFORE the
    /// attribute references are built, so they get the same scale.
    /// </summary>
    public static BlockReference InsertBlock(Transaction tr, Database db, ObjectId btrId, Point3d position,
        double scale = 1.0, double rotation = 0.0, string? layer = null,
        IDictionary<string, string>? attributeValues = null, ObjectId spaceId = default,
        string? annotationScale = null)
    {
        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

        var br = new BlockReference(position, btrId);
        br.SetDatabaseDefaults(db);
        br.ScaleFactors = new Scale3d(scale);
        br.Rotation = rotation;                             // radians
        if (layer is not null)
            br.Layer = layer;

        ObjectId target = spaceId.IsNull ? SymbolUtilityServices.GetBlockModelSpaceId(db) : spaceId;
        var space = (BlockTableRecord)tr.GetObject(target, OpenMode.ForWrite);
        space.AppendEntity(br);
        tr.AddNewlyCreatedDBObject(br, true);

        if (annotationScale is not null)
            ScaleHelper.SetSingleObjectScale(db, br, annotationScale);

        AddAttributeReferences(tr, db, br, btr, attributeValues, annotationScale);
        return br;
    }

    public static ObjectId InsertBlock(Database db, string blockName, Point3d position, double scale = 1.0,
        double rotation = 0.0, string? layer = null, IDictionary<string, string>? attributeValues = null)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        ObjectId btrId = GetBlockId(tr, db, blockName);
        if (btrId.IsNull)
            throw new ArgumentException($"Block '{blockName}' not found.", nameof(blockName));

        BlockReference br = InsertBlock(tr, db, btrId, position, scale, rotation, layer, attributeValues);
        tr.Commit();
        return br.ObjectId;
    }

    /// <summary>A new BlockReference has no attributes: create them from the (non-constant) definitions.</summary>
    private static void AddAttributeReferences(Transaction tr, Database db, BlockReference br, BlockTableRecord btr,
        IDictionary<string, string>? values, string? annotationScale)
    {
        if (!btr.HasAttributeDefinitions)
            return;

        foreach (ObjectId id in btr)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition ad || ad.Constant)
                continue;                                   // constant attributes live only in the definition

            var ar = new AttributeReference();
            ar.SetAttributeFromBlock(ad, br.BlockTransform);
            ar.Position = ad.Position.TransformBy(br.BlockTransform);
            // no [NotNullWhen] attribute in net48 -> use ?? instead of relying on the bool result
            ar.TextString = TryGetIgnoreCase(values, ad.Tag, out string? value) ? value ?? "" : ad.TextString;

            if (ar.IsMTextAttribute)
                ar.UpdateMTextAttribute();

            br.AttributeCollection.AppendAttribute(ar);
            tr.AddNewlyCreatedDBObject(ar, true);

            if (annotationScale is not null && ar.Annotative == AnnotativeStates.True)
                ScaleHelper.SetSingleObjectScale(db, ar, annotationScale);
        }
    }

    private static bool TryGetIgnoreCase(IDictionary<string, string>? dict, string key, out string? value)
    {
        value = null;
        if (dict is null)
            return false;

        foreach (KeyValuePair<string, string> kv in dict)   // no KeyValuePair deconstruction in net48
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }
        return false;
    }

    // ---------- 6. Find references ----------

    /// <summary>Real block name, also for dynamic blocks (anonymous *U definitions).</summary>
    public static string GetEffectiveBlockName(Transaction tr, BlockReference br)
    {
        ObjectId id = br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord;
        return ((BlockTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name;
    }

    /// <summary>All references to the block in model space and layouts.</summary>
    public static List<ObjectId> FindBlockReferences(Database db, string blockName)
    {
        var result = new List<ObjectId>();
        RXClass refClass = RXObject.GetClass(typeof(BlockReference));

        using Transaction tr = db.TransactionManager.StartTransaction();
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

        foreach (ObjectId btrId in bt)
        {
            var space = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            if (!space.IsLayout)
                continue;

            foreach (ObjectId entId in space)
            {
                if (!entId.ObjectClass.IsDerivedFrom(refClass))
                    continue;                               // cheap check without opening

                var br = (BlockReference)tr.GetObject(entId, OpenMode.ForRead);
                if (string.Equals(GetEffectiveBlockName(tr, br), blockName, StringComparison.OrdinalIgnoreCase))
                    result.Add(entId);
            }
        }
        return result;
    }

    public static int EraseAllReferences(Database db, string blockName)
    {
        int count = 0;
        foreach (ObjectId id in FindBlockReferences(db, blockName))
        {
            try
            {
                if (PrimitivesHelper.EraseEntity(db, id))
                    count++;
            }
            catch (AcException ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
            {
                // skip
            }
        }
        return count;
    }

    // ---------- 7. Attributes ----------

    public static Dictionary<string, string> GetAttributeValues(Transaction tr, BlockReference br)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (ObjectId attId in br.AttributeCollection)
        {
            if (tr.GetObject(attId, OpenMode.ForRead) is AttributeReference ar)
                result[ar.Tag] = ar.TextString;
        }
        return result;                                      // constant attributes are NOT here
    }

    public static bool SetAttributeValue(Transaction tr, BlockReference br, string tag, string value)
    {
        foreach (ObjectId attId in br.AttributeCollection)
        {
            var ar = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
            if (!string.Equals(ar.Tag, tag, StringComparison.OrdinalIgnoreCase))
                continue;

            ar.UpgradeOpen();
            ar.TextString = value;
            if (ar.IsMTextAttribute)
                ar.UpdateMTextAttribute();
            return true;
        }
        return false;
    }

    public static bool SetAttributeValue(Database db, ObjectId refId, string tag, string value)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        var br = (BlockReference)tr.GetObject(refId, OpenMode.ForRead);
        bool ok = SetAttributeValue(tr, br, tag, value);
        if (ok)
            tr.Commit();
        return ok;
    }

    /// <summary>
    /// ATTSYNC equivalent for direct references: removes obsolete attributes, adds missing ones,
    /// re-applies definition properties and keeps the values. Matching is by tag.
    /// </summary>
    public static void SyncAttributes(Transaction tr, BlockTableRecord btr)
    {
        var defs = new Dictionary<string, AttributeDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (ObjectId id in btr)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is AttributeDefinition { Constant: false } ad)
                defs[ad.Tag] = ad;
        }

        foreach (ObjectId refId in btr.GetBlockReferenceIds(true, false))
        {
            var br = (BlockReference)tr.GetObject(refId, OpenMode.ForWrite, false, true);
            var attIds = br.AttributeCollection.Cast<ObjectId>().ToList();   // copy: the collection changes
            var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ObjectId attId in attIds)
            {
                var ar = (AttributeReference)tr.GetObject(attId, OpenMode.ForWrite, false, true);
                if (!defs.TryGetValue(ar.Tag, out AttributeDefinition? ad))
                {
                    ar.Erase(true);                         // obsolete attribute
                    continue;
                }

                present.Add(ar.Tag);
                string value = ar.TextString;               // keep the user's value
                ar.SetAttributeFromBlock(ad, br.BlockTransform);
                ar.TextString = value;
                if (ar.IsMTextAttribute)
                    ar.UpdateMTextAttribute();
            }

            foreach (KeyValuePair<string, AttributeDefinition> kv in defs)
            {
                if (present.Contains(kv.Key))
                    continue;

                var ar = new AttributeReference();
                ar.SetAttributeFromBlock(kv.Value, br.BlockTransform);
                ar.Position = kv.Value.Position.TransformBy(br.BlockTransform);
                ar.TextString = kv.Value.TextString;
                br.AttributeCollection.AppendAttribute(ar);
                tr.AddNewlyCreatedDBObject(ar, true);
            }
        }
    }

    public static bool AddAttributeDefinition(Database db, string blockName, AttributeDefinition ad, bool updateReferences = true)
    {
        using Transaction tr = db.TransactionManager.StartTransaction();
        ObjectId id = GetBlockId(tr, db, blockName);
        if (id.IsNull)
        {
            ad.Dispose();
            return false;
        }

        var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForWrite);
        btr.AppendEntity(ad);
        tr.AddNewlyCreatedDBObject(ad, true);

        if (updateReferences)
            SyncAttributes(tr, btr);

        tr.Commit();
        return true;
    }

    // ---------- 8. Annotative blocks ----------

    public static bool IsBlockAnnotative(Transaction tr, ObjectId btrId) =>
        ((BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead)).Annotative == AnnotativeStates.True;

    /// <summary>
    /// Inserts an annotative block with exactly one annotation scale (CANNOSCALE if scaleName is null).
    /// The reference's own scale stays 1 — the apparent size comes from the annotation scale.
    /// </summary>
    public static BlockReference InsertAnnotativeBlock(Transaction tr, Database db, ObjectId btrId, Point3d position,
        string? scaleName = null, double rotation = 0.0, string? layer = null,
        IDictionary<string, string>? attributeValues = null)
    {
        if (!IsBlockAnnotative(tr, btrId))
            throw new InvalidOperationException("The block is not annotative.");

        return InsertBlock(tr, db, btrId, position, 1.0, rotation, layer, attributeValues,
            default, scaleName ?? db.Cannoscale.Name);
    }

    /// <summary>Makes a block annotative and gives every existing reference the scale.</summary>
    public static int ConvertBlockToAnnotative(Database db, string blockName, string? scaleName = null)
    {
        string scale = scaleName ?? db.Cannoscale.Name;

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            ObjectId id = GetBlockId(tr, db, blockName);
            if (id.IsNull)
                return 0;

            var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForWrite);
            btr.BlockScaling = BlockScaling.Uniform;
            btr.Annotative = AnnotativeStates.True;
            tr.Commit();
        }

        int count = 0;
        foreach (ObjectId refId in FindBlockReferences(db, blockName))
        {
            using Transaction tr = db.TransactionManager.StartTransaction();
            var br = (BlockReference)tr.GetObject(refId, OpenMode.ForWrite, false, true);
            if (ScaleHelper.AddScaleToObject(db, br, scale))
                count++;
            tr.Commit();
        }
        return count;
    }
}
