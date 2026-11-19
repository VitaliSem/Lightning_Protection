using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using LP.Plugin.Models;

namespace LP.Plugin.Acad;

public static class BlockHelper
{
    public static ObjectId GetBlockId(Transaction tr, Database db, string blockName)
    {
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        
        return bt.Has(blockName) ? bt[blockName] : ObjectId.Null;
    }

    public static bool IsBlockAnnotative(Transaction tr, ObjectId btrId) =>
        ((BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead)).Annotative == AnnotativeStates.True;


    /// <summary>Returns an existing block definition or creates it from the given entities.</summary>
    public static ObjectId EnsureBlock(Transaction tr, Database db, BlockDefinitionDto block)
    {
        SymbolUtilityServices.ValidateSymbolName(block.Name, false);

        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

        if (bt.Has(block.Name))
        {
            foreach (Entity e in block.Entities)
                e.Dispose();

            return bt[block.Name];
        }

        var btr = new BlockTableRecord { Name = block.Name, Origin = block.Origin };

        if (block.Description is not null)
            btr.Comments = block.Description;

        if (block.Annotative)
        {
            btr.BlockScaling = BlockScaling.Uniform;
            btr.Annotative = AnnotativeStates.True;
        }

        bt.UpgradeOpen();
        ObjectId btrId = bt.Add(btr);
        tr.AddNewlyCreatedDBObject(btr, true);

        if (!block.Annotative)
        {
            AppendEntities(tr, btr, block.Entities);
            return btrId;
        }

        string oneToOne = ScaleHelper.EnsureOneToOneScale(db);

        ScaleHelper.RunAtScale(db, oneToOne, () =>
        {
            AppendEntities(tr, btr, block.Entities);

            foreach (var ent in block.Entities)
            {
                if (ent is not AttributeDefinition) continue;

                ent.Annotative = AnnotativeStates.True;
                ScaleHelper.SetSingleObjectScale(db, ent, oneToOne);
            }
        });

        return btrId;
    }

    private static void AppendEntities(Transaction tr, BlockTableRecord btr, IEnumerable<Entity> entities)
    {
        foreach (var ent in entities)
        {
            btr.AppendEntity(ent);
            tr.AddNewlyCreatedDBObject(ent, true);
        }
    }

    /// <summary>Inserts a block reference together with its attribute references.</summary>
    public static BlockReference InsertBlock(Transaction tr, Database db, BlockInsertDto insert)
    {
        var br = new BlockReference(insert.Position, insert.BlockId);

        br.SetDatabaseDefaults(db);
        br.ScaleFactors = new Scale3d(insert.Scale);
        br.Rotation = insert.Rotation;

        if (insert.Layer is not null)
            br.Layer = insert.Layer;

        ObjectId target = insert.SpaceId.IsNull ? SymbolUtilityServices.GetBlockModelSpaceId(db) : insert.SpaceId;

        var space = (BlockTableRecord)tr.GetObject(target, OpenMode.ForWrite);

        space.AppendEntity(br);
        tr.AddNewlyCreatedDBObject(br, true);

        if (insert.AnnotationScale is not null)
            ScaleHelper.SetSingleObjectScale(db, br, insert.AnnotationScale);

        BlockAttributeHelper.AddAttributeReferences(tr, br, insert);

        return br;
    }

    /// <summary>Inserts an annotative block reference sized by a single annotation scale.</summary>
    public static BlockReference InsertAnnotativeBlock(Transaction tr, Database db, BlockInsertDto insert)
    {
        if (!IsBlockAnnotative(tr, insert.BlockId))
            throw new InvalidOperationException("The block is not annotative.");

        insert.Scale = 1.0;
        insert.AnnotationScale ??= db.Cannoscale.Name;

        return InsertBlock(tr, db, insert);
    }

    /// <summary>Returns the name of the block a reference is based on, including dynamic blocks.</summary>
    public static string GetEffectiveBlockName(Transaction tr, BlockReference br)
    {
        ObjectId id = br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord;

        return ((BlockTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name;
    }

    /// <summary>Finds all references to a block in model space and layouts.</summary>
    public static List<ObjectId> FindBlockReferences(Transaction tr, Database db, string blockName)
    {
        var result = new List<ObjectId>();

        RXClass refClass = RXObject.GetClass(typeof(BlockReference));

        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

        foreach (var btrId in bt)
        {
            var space = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

            if (!space.IsLayout)
                continue;

            foreach (ObjectId entId in space)
            {
                if (!entId.ObjectClass.IsDerivedFrom(refClass))
                    continue;

                var br = (BlockReference)tr.GetObject(entId, OpenMode.ForRead);

                if (string.Equals(GetEffectiveBlockName(tr, br), blockName, StringComparison.OrdinalIgnoreCase))
                    result.Add(entId);
            }
        }

        return result;
    }
}
