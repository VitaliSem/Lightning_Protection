using System.Globalization;
using LP.Core.Interfaces;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;
using LP.Core.Services;

namespace LP.UI.App.Services;

/// <summary>
/// Repository that is not connected to a drawing; used to run the window without AutoCAD.
/// Holds sample protectors so the table (empty optional values, paper space, scrolling) can be checked by eye;
/// added protectors are kept in memory, positions cannot be picked.
/// </summary>
public class InMemoryLightningProtectorRepository : ILightningProtectorRepository
{
    private const int SampleCount = 25;

    private readonly List<LightningProtectorListEntry> _entries = CreateSamples();

    public string SourceName => "Not connected to a drawing";

    public OperationResult<IReadOnlyList<LightningProtectorListEntry>> GetAll() =>
        OperationResult<IReadOnlyList<LightningProtectorListEntry>>.Ok(_entries);

    public OperationResult<string> Focus(int number) =>
        OperationResult<string>.Fail($"Not connected to a drawing: lightning protector #{number} cannot be shown.");

    public OperationResult<LightningProtectorBlockData> Insert(LightningProtectorInsertDto dto)
    {
        OperationResult<LightningProtectorBlockData> result = LightningProtectorBlockService.Create(new LightningProtectorBlockCreateDto
        {
            ExistingNumbers = _entries.Select(e => e.Data.Number.ToString(CultureInfo.InvariantCulture)),
            Name = dto.Name,
            Title = dto.Title,
            Type = dto.Type,
            HeightM = dto.HeightM,
            BaseHeightM = dto.BaseHeightM,
            Description = dto.Description,
        });

        if (result.IsSuccess)
            _entries.Add(new LightningProtectorListEntry(result.Result, dto.Position));

        return result;
    }

    public OperationResult<Point3D> PickPosition() =>
        OperationResult<Point3D>.Fail("Not connected to a drawing: enter X and Y instead of picking the position.");

    private static List<LightningProtectorListEntry> CreateSamples()
    {
        var entries = new List<LightningProtectorListEntry>();

        for (var number = 1; number <= SampleCount; number++)
        {
            var withOptionals = number % 3 != 0;

            var data = new LightningProtectorBlockData(
                number,
                withOptionals ? $"MP-{number}" : "",
                withOptionals ? "Rod" : "",
                20 + number % 7 * 2.5,
                number % 5 != 0,
                withOptionals ? number % 4 * 0.5 : null,
                withOptionals
                    ? $"Lightning protector {number} on the roof of building {number % 4 + 1}; mounted on a steel mast and grounded to the main earthing loop."
                    : "",
                withOptionals ? $"Building {number % 4 + 1} roof" : "");

            var onLayout = number > SampleCount - 2;

            entries.Add(new LightningProtectorListEntry(
                data,
                onLayout ? new Point3D(120 + number, 80.5) : new Point3D(1000 + number * 12.75, 2500 - number * 8.4),
                onLayout ? "Layout1" : null));
        }

        return entries;
    }
}
