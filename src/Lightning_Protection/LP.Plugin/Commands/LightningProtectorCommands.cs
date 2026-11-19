using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;
using LP.Core.Services;
using LP.Plugin.Acad;
using LP.Plugin.Constants;
using LP.Plugin.Drawing;
using LP.Plugin.Models;
// Core.Application (AcCoreMgd) exists in both targets; the full Application (AcMgd) is not referenced on net8.0
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcException = Autodesk.AutoCAD.Runtime.Exception;

[assembly: CommandClass(typeof(LP.Plugin.Commands.LightningProtectorCommands))]

namespace LP.Plugin.Commands;

public class LightningProtectorCommands
{
    [CommandMethod(CommandNames.LightningProtectorInsert)]
    public void InsertLightningProtector()
    {
        Document? doc = AcApp.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return;

        Database db = doc.Database;
        Editor ed = doc.Editor;

        try
        {
            if (!PromptHelper.TryGetPoint(ed, LightningProtectorPrompts.InsertionPoint, out Point3d position)) return;
            if (!PromptHelper.TryGetString(ed, LightningProtectorPrompts.Name, out string name)) return;
            if (!PromptHelper.TryGetString(ed, LightningProtectorPrompts.Title, out string title)) return;
            if (!PromptHelper.TryGetString(ed, LightningProtectorPrompts.Type, out string type)) return;
            if (!PromptHelper.TryGetPositiveDouble(ed, LightningProtectorPrompts.Height, out double height)) return;
            if (!PromptHelper.TryGetOptionalDouble(ed, LightningProtectorPrompts.BaseHeight, out double? baseHeight)) return;
            if (!PromptHelper.TryGetString(ed, LightningProtectorPrompts.Description, out string description)) return;

            using Transaction tr = db.TransactionManager.StartTransaction();

            ObjectId btrId = LightningProtectorBlock.EnsureDefinition(tr, db);

            OperationResult<LightningProtectorBlockData> result = LightningProtectorBlockService.Create(
                new LightningProtectorBlockCreateDto
                {
                    ExistingNumbers = GetExistingNumbers(tr, db),
                    Name = name,
                    Title = title,
                    Type = type,
                    HeightM = height,
                    BaseHeightM = baseHeight,
                    Description = description,
                });

            if (!result.IsSuccess)
            {
                ed.WriteMessage("\n" + result.Message);
                return;
            }

            BlockHelper.InsertAnnotativeBlock(tr, db, new BlockInsertDto
            {
                BlockId = btrId,
                Position = position,
                Layer = LightningProtectorBlockService.LayerName,
                AttributeValues = LightningProtectorBlockService.ToAttributeValues(result.Result),
                SpaceId = db.CurrentSpaceId,
                AnnotationScale = db.Cannoscale.Name,
            });

            tr.Commit();
            ed.WriteMessage($"\nLightning protector #{result.Result.Number} inserted.");
        }
        catch (AcException ex)
        {
            ed.WriteMessage($"\nAutoCAD error: {ex.ErrorStatus} - {ex.Message}");
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage("\nError: " + ex.Message);
        }
    }

    private static List<string> GetExistingNumbers(Transaction tr, Database db)
    {
        var numbers = new List<string>();

        foreach (ObjectId refId in BlockHelper.FindBlockReferences(tr, db, LightningProtectorBlockService.BlockName))
        {
            var br = (BlockReference)tr.GetObject(refId, OpenMode.ForRead);

            if (BlockAttributeHelper.GetAttributeValues(tr, br)
                .TryGetValue(LightningProtectorBlockService.AttrNumber, out string? number))
                numbers.Add(number);
        }

        return numbers;
    }
}
