using System.Globalization;
using LP.Core.Models.LightningProtectors.Block;

namespace LP.UI.ViewModels;

public class LightningProtectorItemViewModel(LightningProtectorListEntry entry)
{
    private const string PaperSpaceToolTip = "Paper-space coordinates (layout \"{0}\").";

    private LightningProtectorListEntry Entry { get; } = entry ?? throw new ArgumentNullException(nameof(entry));

    private LightningProtectorBlockData Data => Entry.Data;

    public int Number => Data.Number;

    public string NumberText => "#" + Data.Number.ToString(CultureInfo.InvariantCulture);

    public string Name => Data.Name ?? "";

    public string Title => Data.Title ?? "";

    public string Type => Data.Type ?? "";

    public double HeightM => Data.HeightM;

    public string HeightText => FormatMeters(Data.HeightM);

    public double? BaseHeightM => Data.BaseHeightM;

    public string BaseHeightText => Data.BaseHeightM.HasValue ? FormatMeters(Data.BaseHeightM.Value) : "";

    public string Description => Data.Description ?? "";

    public bool HasDescription => !string.IsNullOrWhiteSpace(Data.Description);

    public bool IsActive => Data.IsActive;

    public string StatusText => Data.IsActive ? "Active" : "Inactive";

    public string DisplayName => string.IsNullOrWhiteSpace(Data.Name) ? NumberText : $"{NumberText} {Data.Name}";

    public double X => Entry.Position?.X ?? 0;

    public double Y => Entry.Position?.Y ?? 0;

    public string PositionText => FormatMeters(X) + "; " + FormatMeters(Y);

    public bool IsInPaperSpace => !string.IsNullOrEmpty(Entry.LayoutName);

    public string PositionToolTip =>
        IsInPaperSpace ? string.Format(CultureInfo.InvariantCulture, PaperSpaceToolTip, Entry.LayoutName) : "";

    public string FocusToolTip => $"Show {NumberText} on the drawing";

    public static string FormatMeters(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
