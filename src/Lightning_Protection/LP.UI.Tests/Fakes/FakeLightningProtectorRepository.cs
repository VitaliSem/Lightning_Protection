using LP.Core.Interfaces;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;

namespace LP.UI.Tests.Fakes;

internal class FakeLightningProtectorRepository : ILightningProtectorRepository
{
    public string SourceName { get; set; } = "Fake";

    public OperationResult<IReadOnlyList<LightningProtectorListEntry>> GetAllResult { get; set; } =
        OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Ok([]);

    public OperationResult<string> FocusResult { get; set; } = OperationResult<string>.Ok("", "Focused.");

    public List<int> FocusedNumbers { get; } = [];

    public OperationResult<LightningProtectorBlockData>? InsertResult { get; set; }

    public List<LightningProtectorInsertDto> Inserted { get; } = [];

    public OperationResult<Point3D> PickResult { get; set; } = OperationResult<Point3D>.Fail("No position was picked.");

    public int PickCount { get; private set; }

    public OperationResult<IReadOnlyList<LightningProtectorListEntry>> GetAll() => GetAllResult;

    public OperationResult<LightningProtectorBlockData> Insert(LightningProtectorInsertDto dto)
    {
        Inserted.Add(dto);

        if (InsertResult is not null)
            return InsertResult;

        List<LightningProtectorListEntry> entries = [.. GetAllResult.Result ?? []];
        var data = new LightningProtectorBlockData(
            entries.Select(e => e.Data.Number).DefaultIfEmpty(0).Max() + 1,
            dto.Name, dto.Type, dto.HeightM, true, dto.BaseHeightM, dto.Description, dto.Title);
        entries.Add(new LightningProtectorListEntry(data, dto.Position));
        GetAllResult = OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Ok(entries);

        return OperationResult<LightningProtectorBlockData>.Ok(data);
    }

    public OperationResult<Point3D> PickPosition()
    {
        PickCount++;
        return PickResult;
    }

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
