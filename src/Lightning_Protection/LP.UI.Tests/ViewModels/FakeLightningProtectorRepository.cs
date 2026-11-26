using LP.Core.Interfaces;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;

namespace LP.UI.Tests.ViewModels;

internal class FakeLightningProtectorRepository : ILightningProtectorRepository
{
    public string SourceName { get; set; } = "Fake";

    public OperationResult<IReadOnlyList<LightningProtectorListEntry>> GetAllResult { get; set; } =
        OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Ok([]);

    public OperationResult<string> FocusResult { get; set; } = OperationResult<string>.Ok("", "Focused.");

    public List<int> FocusedNumbers { get; } = [];

    public OperationResult<IReadOnlyList<LightningProtectorListEntry>> GetAll() => GetAllResult;

    public OperationResult<string> Focus(int number)
    {
        FocusedNumbers.Add(number);
        return FocusResult;
    }

    public void SetEntries(params LightningProtectorListEntry[] entries) =>
        GetAllResult = OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Ok(entries);

    public static LightningProtectorListEntry Entry(int number, bool isActive = true, string? layoutName = null,
        double x = 0, double y = 0) =>
        new(new LightningProtectorBlockData(number, "MP-" + number, "Rod", 20, isActive), new Point3D(x, y), layoutName);
}
