using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using LP.Core.Models.Base;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcException = Autodesk.AutoCAD.Runtime.Exception;

namespace LP.Plugin.Acad;

public static class ViewHelper
{
    private const string CvportVariable = "CVPORT";
    private const short PaperSpaceViewport = 1;

    public static string GetLayoutName(Transaction tr, ObjectId spaceId)
    {
        var space = (BlockTableRecord)tr.GetObject(spaceId, OpenMode.ForRead);
        var layout = (Layout)tr.GetObject(space.LayoutId, OpenMode.ForRead);

        return layout.LayoutName;
    }

    /// <summary>
    /// Makes the given layout current; on a paper-space layout also leaves a floating viewport for paper space.
    /// Must be called outside an open transaction.
    /// </summary>
    public static void ActivateLayout(Database db, Editor ed, string layoutName)
    {
        LayoutManager lm = LayoutManager.Current;

        if (!string.Equals(lm.CurrentLayout, layoutName, StringComparison.OrdinalIgnoreCase))
            lm.CurrentLayout = layoutName;

        if (!db.TileMode
            && Convert.ToInt16(AcApp.GetSystemVariable(CvportVariable), CultureInfo.InvariantCulture) != PaperSpaceViewport)
            ed.SwitchToPaperSpace();
    }

    /// <summary>Returns the entity's extents, or a zero-size extent at the fallback point when it has none.</summary>
    public static Extents3d GetExtentsOrPoint(Entity entity, Point3d fallback)
    {
        try
        {
            return entity.GeometricExtents;
        }
        catch (AcException)
        {
            return new Extents3d(fallback, fallback);
        }
    }

    public static double GetViewAspectRatio(Editor ed)
    {
        using ViewTableRecord view = ed.GetCurrentView();

        return view.Width / view.Height;
    }

    /// <summary>Sets the current view to a window given in WCS, taking the view direction and twist into account.</summary>
    public static void ZoomToWindow(Editor ed, ViewWindow window)
    {
        using ViewTableRecord view = ed.GetCurrentView();

        Matrix3d worldToEye = (Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target)
                               * Matrix3d.Displacement(view.Target - Point3d.Origin)
                               * Matrix3d.PlaneToWorld(view.ViewDirection)).Inverse();

        Point3d center = new Point3d(window.CenterX, window.CenterY, 0).TransformBy(worldToEye);

        view.CenterPoint = new Point2d(center.X, center.Y);
        view.Width = window.Width;
        view.Height = window.Height;

        ed.SetCurrentView(view);
    }
}
