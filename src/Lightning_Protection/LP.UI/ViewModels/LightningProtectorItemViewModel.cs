using System.Globalization;
using LP.Core.Models.LightningProtectors.Block;

namespace LP.UI.ViewModels;

public class LightningProtectorItemViewModel(LightningProtectorBlockData data)
{
    private LightningProtectorBlockData Data { get; } = data ?? throw new ArgumentNullException(nameof(data));

    public int Number => Data.Number;

    public string NumberText => "#" + Data.Number.ToString(CultureInfo.InvariantCulture);

    public string Name => Data.Name ?? "";

    public string Title => Data.Title ?? "";

    public string Type => Data.Type ?? "";

    public string HeightText => FormatMeters(Data.HeightM);

    public string BaseHeightText => Data.BaseHeightM.HasValue ? FormatMeters(Data.BaseHeightM.Value) : "";

    public string Description => Data.Description ?? "";

    public bool HasDescription => !string.IsNullOrWhiteSpace(Data.Description);

    public bool IsActive => Data.IsActive;

    public string StatusText => Data.IsActive ? "Active" : "Inactive";

    public string DisplayName => string.IsNullOrWhiteSpace(Data.Name) ? NumberText : $"{NumberText} {Data.Name}";

    public static string FormatMeters(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
