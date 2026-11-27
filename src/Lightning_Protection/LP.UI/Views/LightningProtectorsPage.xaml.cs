using System.Windows.Controls;

namespace LP.UI.Views;

public partial class LightningProtectorsPage : UserControl
{
    public LightningProtectorsPage()
    {
        InitializeComponent();

        // Keeps a row selected by the view model (e.g. a newly added protector) in view.
        ProtectorsGrid.SelectionChanged += (_, _) =>
        {
            if (ProtectorsGrid.SelectedItem is not null)
                ProtectorsGrid.ScrollIntoView(ProtectorsGrid.SelectedItem);
        };
    }
}
