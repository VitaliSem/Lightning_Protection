using LP.Core.Interfaces;

namespace LP.UI.App.Services;

/// <summary>Repository that is not connected to a drawing; used to run the window without AutoCAD.</summary>
public class InMemoryLightningProtectorRepository : ILightningProtectorRepository
{
    public string SourceName => "Not connected to a drawing";
}
