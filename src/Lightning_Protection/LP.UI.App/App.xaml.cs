using System.Windows;
using LP.UI.App.Services;

namespace LP.UI.App;

// Standalone host: runs the window without AutoCAD.
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new MainWindow(new InMemoryLightningProtectorRepository());
        MainWindow = window;
        window.Show();
    }
}
