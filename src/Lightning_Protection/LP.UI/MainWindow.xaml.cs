using System.Windows;
using LP.Core.Interfaces;
using LP.UI.Services;
using LP.UI.ViewModels;

namespace LP.UI;

public partial class MainWindow : Window
{
    public MainWindow(ILightningProtectorRepository repository)
    {
        MainViewModel viewModel;
        InitializeComponent();
        DataContext = viewModel = new MainViewModel(repository, new DialogService(this));

        // The source can change while the window is inactive (drawing edited or switched in AutoCAD).
        Activated += (_, _) => viewModel.Refresh();
    }
}
