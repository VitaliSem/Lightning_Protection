using LP.Core.Interfaces;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;

namespace LP.UI.App.Services;

/// <summary>
/// Repository that is not connected to a drawing; used to run the window without AutoCAD.
/// Holds sample protectors so the table (empty optional values, paper space, scrolling) can be checked by eye.
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
