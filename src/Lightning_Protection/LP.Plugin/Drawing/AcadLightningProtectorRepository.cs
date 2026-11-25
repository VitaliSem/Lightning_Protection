using Autodesk.AutoCAD.ApplicationServices;
using LP.Core.Interfaces;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace LP.Plugin.Drawing;

/// <summary>Lightning protectors of the active drawing for the UI window.</summary>
public class AcadLightningProtectorRepository : ILightningProtectorRepository
{
    private const string NoDrawingMessage = "No drawing is open.";

    public string SourceName
    {
        get
        {
            Document? doc = AcApp.DocumentManager.MdiActiveDocument;

            return doc is null ? NoDrawingMessage : "Drawing: " + System.IO.Path.GetFileName(doc.Name);
        }
    }
}
