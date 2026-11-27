using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using LP.Core.Constants;
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
    [CommandMethod(CommandNames.LightningProtector.Insert)]
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
            if (!PromptHelper.TryGetOptionalKeyword(ed, LightningProtectorPrompts.TypeChoice, LightningProtectorPrompts.TypeKeywords, out string? typeKeyword)) return;
            if (!PromptHelper.TryGetPositiveDouble(ed, LightningProtectorPrompts.Height, out double height)) return;
            if (!PromptHelper.TryGetOptionalDouble(ed, LightningProtectorPrompts.BaseHeight, out double? baseHeight)) return;
            if (!PromptHelper.TryGetString(ed, LightningProtectorPrompts.Description, out string description)) return;

            using Transaction tr = db.TransactionManager.StartTransaction();

            OperationResult<LightningProtectorBlockData> result = LightningProtectorInsert.Insert(tr, db,
                new LightningProtectorInsertDto
                {
                    Position = new Point3D(position.X, position.Y, position.Z),
                    Name = name,
                    Title = title,
                    Type = LightningProtectorPrompts.TypeFromKeyword(typeKeyword),
                    HeightM = height,
                    BaseHeightM = baseHeight,
                    Description = description,
                });

            if (!result.IsSuccess)
            {
                ed.WriteMessage("\n" + result.Message);
                return;
            }

            tr.Commit();
            ed.WriteMessage(string.Format(LightningProtectorPrompts.Inserted, result.Result.Number));
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

    [CommandMethod(CommandNames.LightningProtector.Edit)]
    public void EditLightningProtector()
    {
        Document? doc = AcApp.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return;

        Database db = doc.Database;
        Editor ed = doc.Editor;

        try
        {
            if (!PromptHelper.TryGetPositiveInt(ed, LightningProtectorPrompts.Number, out var number)) return;

            OperationResult<LightningProtectorReferenceDto> found;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                found = LightningProtectorFinder.FindByNumber(tr, db, number);
                tr.Commit();
            }

            if (!found.IsSuccess)
            {
                ed.WriteMessage("\n" + found.Message);
                return;
            }

            ObjectId refId = found.Result.Id;

            OperationResult<LightningProtectorBlockData> current =
                LightningProtectorBlockService.FromAttributeValues(found.Result.AttributeValues);

            if (!current.IsSuccess)
            {
                ed.WriteMessage("\n" + current.Message);
                return;
            }

            LightningProtectorBlockData data = current.Result;
            var isActive = data.IsActive;

            if (!PromptHelper.TryGetString(ed, LightningProtectorPrompts.EditName(data.Name), out string name)) return;
            if (!PromptHelper.TryGetString(ed, LightningProtectorPrompts.EditTitle(data.Title), out string title)) return;
            if (!PromptHelper.TryGetString(ed, LightningProtectorPrompts.EditType(data.Type), out string type)) return;
            if (!PromptHelper.TryGetPositiveDouble(ed, LightningProtectorPrompts.EditHeight(data.HeightM), out double height)) return;
            if (!PromptHelper.TryGetClearableDouble(ed, LightningProtectorPrompts.EditBaseHeight(data.BaseHeightM), out double? baseHeight)) return;
            if (!PromptHelper.TryGetString(ed, LightningProtectorPrompts.EditDescription(data.Description), out string description)) return;
            if (!PromptHelper.TryGetYesNo(ed, LightningProtectorPrompts.IsActive, ref isActive)) return;

            OperationResult<LightningProtectorBlockData> result = LightningProtectorBlockService.Update(data,
                new LightningProtectorBlockUpdateDto
                {
                    Name = name,
                    Title = title,
                    Type = type,
                    HeightM = height,
                    BaseHeightM = baseHeight,
                    Description = description,
                    IsActive = isActive,
                });

            if (!result.IsSuccess)
            {
                ed.WriteMessage("\n" + result.Message);
                return;
            }

            IDictionary<string, string> newValues = LightningProtectorBlockService.ToAttributeValues(result.Result);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(refId, OpenMode.ForRead);

                BlockAttributeHelper.SetAttributeValues(tr, br, LightningProtectorAttributes.EditableTags
                    .ToDictionary(tag => tag, tag => newValues[tag], StringComparer.OrdinalIgnoreCase));

                tr.Commit();
            }

            ed.WriteMessage(string.Format(LightningProtectorPrompts.Updated, number));
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

    [CommandMethod(CommandNames.LightningProtector.ToggleActive)]
    public void ToggleLightningProtectorActive()
    {
        Document? doc = AcApp.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return;

        Database db = doc.Database;
        Editor ed = doc.Editor;

        try
        {
            if (!PromptHelper.TryGetPositiveInt(ed, LightningProtectorPrompts.Number, out var number)) return;

            using Transaction tr = db.TransactionManager.StartTransaction();

            OperationResult<LightningProtectorReferenceDto> found = LightningProtectorFinder.FindByNumber(tr, db, number);

            if (!found.IsSuccess)
            {
                ed.WriteMessage("\n" + found.Message);
                return;
            }

            OperationResult<bool> toggled = LightningProtectorBlockService.ToggleActive(found.Result.AttributeValues);

            if (!toggled.IsSuccess)
            {
                ed.WriteMessage("\n" + toggled.Message);
                return;
            }

            var br = (BlockReference)tr.GetObject(found.Result.Id, OpenMode.ForRead);

            BlockAttributeHelper.SetAttributeValues(tr, br, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { LightningProtectorAttributes.IsActive, LightningProtectorBlockService.FormatIsActive(toggled.Result) },
            });

            tr.Commit();
            ed.WriteMessage(string.Format(toggled.Result ? LightningProtectorPrompts.Activated : LightningProtectorPrompts.Deactivated, number));
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

    [CommandMethod(CommandNames.LightningProtector.Delete)]
    public void DeleteLightningProtector()
    {
        Document? doc = AcApp.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return;

        Database db = doc.Database;
        Editor ed = doc.Editor;

        try
        {
            if (!PromptHelper.TryGetPositiveInt(ed, LightningProtectorPrompts.Number, out var number)) return;

            OperationResult<LightningProtectorReferenceDto> found;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                found = LightningProtectorFinder.FindByNumber(tr, db, number);
                tr.Commit();
            }

            if (!found.IsSuccess)
            {
                ed.WriteMessage("\n" + found.Message);
                return;
            }

            found.Result.AttributeValues.TryGetValue(LightningProtectorAttributes.Name, out var name);

            var confirmed = false;

            if (!PromptHelper.TryGetYesNo(ed, LightningProtectorPrompts.ConfirmDelete(number, name ?? ""), ref confirmed) || !confirmed)
            {
                ed.WriteMessage(string.Format(LightningProtectorPrompts.DeleteCancelled, number));
                return;
            }
            
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(found.Result.Id, OpenMode.ForWrite);
                br.Erase();
                tr.Commit();
            }

            ed.WriteMessage(string.Format(LightningProtectorPrompts.Deleted, number));
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

    [CommandMethod(CommandNames.LightningProtector.Focus, CommandFlags.Redraw)]
    public void FocusLightningProtector()
    {
        Document? doc = AcApp.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return;

        Database db = doc.Database;
        Editor ed = doc.Editor;

        try
        {
            if (!PromptHelper.TryGetPositiveInt(ed, LightningProtectorPrompts.Number, out var number)) return;

            OperationResult<string> focused = LightningProtectorFocus.Focus(doc, number);

            ed.WriteMessage("\n" + focused.Message);
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
}
