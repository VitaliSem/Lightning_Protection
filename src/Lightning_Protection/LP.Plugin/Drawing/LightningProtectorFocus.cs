using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using LP.Core.Models.Base;
using LP.Core.Services;
using LP.Plugin.Acad;
using LP.Plugin.Models;

namespace LP.Plugin.Drawing;

/// <summary>Brings a lightning protector into view; shared by the focus command and the UI window.</summary>
public static class LightningProtectorFocus
{
    private const string FocusedMessage = "Lightning protector #{0} in focus.";
    private const string HiddenLayerMessage = "Layer \"{0}\" of the lightning protector is off or frozen, so the block is not visible.";

    /// <summary>
    /// Makes the space that owns the protector current, zooms to it and leaves it selected.
    /// Must be called outside an open transaction; the drawing itself is not modified.
    /// </summary>
    public static OperationResult<string> Focus(Document doc, int number)
    {
        Database db = doc.Database;
        Editor ed = doc.Editor;

        ObjectId refId;
        Extents3d extents;
        string layoutName;
        string? hiddenLayer = null;

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            OperationResult<LightningProtectorReferenceDto> found = LightningProtectorFinder.FindByNumber(tr, db, number);

            if (!found.IsSuccess)
                return OperationResult<string>.Fail(found.Message);

            refId = found.Result.Id;

            var br = (BlockReference)tr.GetObject(refId, OpenMode.ForRead);
            extents = ViewHelper.GetExtentsOrPoint(br, br.Position);
            layoutName = ViewHelper.GetLayoutName(tr, br.OwnerId);

            var layer = (LayerTableRecord)tr.GetObject(br.LayerId, OpenMode.ForRead);

            if (layer.IsOff || layer.IsFrozen)
                hiddenLayer = layer.Name;

            tr.Commit();
        }

        ViewHelper.ActivateLayout(db, ed, layoutName);

        OperationResult<ViewWindow> window = ViewFocusService.CalculateFocusWindow(new ViewFocusDto
        {
            Min = new Point3D(extents.MinPoint.X, extents.MinPoint.Y, extents.MinPoint.Z),
            Max = new Point3D(extents.MaxPoint.X, extents.MaxPoint.Y, extents.MaxPoint.Z),
            AspectRatio = ViewHelper.GetViewAspectRatio(ed),
        });

        if (!window.IsSuccess)
            return OperationResult<string>.Fail(window.Message);

        ViewHelper.ZoomToWindow(ed, window.Result);
        ed.SetImpliedSelection([refId]);

        string message = string.Format(CultureInfo.InvariantCulture, FocusedMessage, number);

        if (hiddenLayer is not null)
            message += "\n" + string.Format(CultureInfo.InvariantCulture, HiddenLayerMessage, hiddenLayer);

        return OperationResult<string>.Ok(message, message);
    }
}
