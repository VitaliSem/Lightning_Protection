using System.Windows;
using LP.UI.ViewModels;

namespace LP.UI.Views;

public partial class AddLightningProtectorDialog : Window
{
    public AddLightningProtectorDialog(AddLightningProtectorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        EventHandler<bool> close = (_, result) => DialogResult = result;
        viewModel.CloseRequested += close;
        Closed += (_, _) => viewModel.CloseRequested -= close;
    }
}
