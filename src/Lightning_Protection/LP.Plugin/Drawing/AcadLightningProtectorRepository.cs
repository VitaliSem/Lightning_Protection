using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using LP.Core.Interfaces;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;
using LP.Core.Services;
using LP.Plugin.Acad;
using LP.Plugin.Commands;
using LP.Plugin.Models;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcException = Autodesk.AutoCAD.Runtime.Exception;
// SetFocusToDwgView lives in AcMgd, referenced through the AutoCAD.NET package
using AcUtils = Autodesk.AutoCAD.Internal.Utils;

namespace LP.Plugin.Drawing;

/// <summary>
/// Lightning protectors of the active drawing for the UI window.
/// The window is modeless, so every access to the drawing takes the document lock.
/// </summary>
public class AcadLightningProtectorRepository : ILightningProtectorRepository
{
    private const string NoDrawingMessage = "No drawing is open.";
    private const string NoPointPickedMessage = "No position was picked on the drawing.";

    public string SourceName
    {
        get
        {
            Document? doc = AcApp.DocumentManager.MdiActiveDocument;

            return doc is null ? NoDrawingMessage : "Drawing: " + System.IO.Path.GetFileName(doc.Name);
        }
    }

    public OperationResult<IReadOnlyList<LightningProtectorListEntry>> GetAll()
    {
        Document? doc = AcApp.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Ok([]);

        try
        {
            List<LightningProtectorReferenceDto> references;

            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                references = LightningProtectorFinder.FindAll(tr, doc.Database);
                tr.Commit();
            }

            var entries = new List<LightningProtectorListEntry>();

            foreach (LightningProtectorReferenceDto reference in references)
            {
                OperationResult<LightningProtectorBlockData> data = LightningProtectorBlockService.FromAttributeValues(reference.AttributeValues);

                if (!data.IsSuccess)
                    continue;

                entries.Add(new LightningProtectorListEntry(
                    data.Result,
                    new Point3D(reference.Position.X, reference.Position.Y, reference.Position.Z),
                    reference.LayoutName));
            }

            return OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Ok(
                entries.OrderBy(e => e.Data.Number).ToList());
        }
        catch (AcException ex)
        {
            return OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Fail(
                $"AutoCAD error while reading lightning protectors: {ex.ErrorStatus} - {ex.Message}", ex);
        }
        catch (System.Exception ex)
        {
            return OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Fail(
                "Error while reading lightning protectors: " + ex.Message, ex);
        }
    }

    public OperationResult<string> Focus(int number)
    {
        Document? doc = AcApp.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return OperationResult<string>.Fail(NoDrawingMessage);

        try
        {
            OperationResult<string> focused;

            using (doc.LockDocument())
                focused = LightningProtectorFocus.Focus(doc, number);

            if (focused.IsSuccess)
                AcUtils.SetFocusToDwgView();

            return focused;
        }
        catch (AcException ex)
        {
            return OperationResult<string>.Fail($"AutoCAD error: {ex.ErrorStatus} - {ex.Message}", ex);
        }
        catch (System.Exception ex)
        {
            return OperationResult<string>.Fail("Error: " + ex.Message, ex);
        }
    }

    public OperationResult<LightningProtectorBlockData> Insert(LightningProtectorInsertDto dto)
    {
        Document? doc = AcApp.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return OperationResult<LightningProtectorBlockData>.Fail(NoDrawingMessage);

        try
        {
            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                OperationResult<LightningProtectorBlockData> result = LightningProtectorInsert.Insert(tr, doc.Database, dto);

                if (result.IsSuccess)
                    tr.Commit();

                return result;
            }
        }
        catch (AcException ex)
        {
            return OperationResult<LightningProtectorBlockData>.Fail(
                $"AutoCAD error while inserting the lightning protector: {ex.ErrorStatus} - {ex.Message}", ex);
        }
        catch (System.Exception ex)
        {
            return OperationResult<LightningProtectorBlockData>.Fail(
                "Error while inserting the lightning protector: " + ex.Message, ex);
        }
    }

    public OperationResult<Point3D> PickPosition()
    {
        Document? doc = AcApp.DocumentManager.MdiActiveDocument;

        if (doc is null)
            return OperationResult<Point3D>.Fail(NoDrawingMessage);

        try
        {
            AcUtils.SetFocusToDwgView();

            using (doc.LockDocument())
            {
                if (!PromptHelper.TryGetPoint(doc.Editor, LightningProtectorPrompts.InsertionPoint, out Point3d point))
                    return OperationResult<Point3D>.Fail(NoPointPickedMessage);

                return OperationResult<Point3D>.Ok(new Point3D(point.X, point.Y, point.Z));
            }
        }
        catch (AcException ex)
        {
            return OperationResult<Point3D>.Fail($"AutoCAD error: {ex.ErrorStatus} - {ex.Message}", ex);
        }
        catch (System.Exception ex)
        {
            return OperationResult<Point3D>.Fail("Error: " + ex.Message, ex);
        }
    }
}
