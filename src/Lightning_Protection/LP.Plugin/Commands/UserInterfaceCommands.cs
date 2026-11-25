using System.Windows;
using Autodesk.AutoCAD.Runtime;
using LP.Plugin.Constants;
using LP.Plugin.Drawing;
using LP.UI;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
// ShowModelessWindow lives in the full Application (AcMgd), referenced through the AutoCAD.NET package
using AcUiApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(LP.Plugin.Commands.UserInterfaceCommands))]

namespace LP.Plugin.Commands;

public class UserInterfaceCommands
{
    // One window per AutoCAD session; it always works on the active drawing.
    private static MainWindow? _window;

    [CommandMethod(CommandNames.UserInterface.Show)]
    public void ShowLightningProtectionUi()
    {
        try
        {
            if (_window is not null)
            {
                if (_window.WindowState == WindowState.Minimized)
                    _window.WindowState = WindowState.Normal;

                _window.Activate();
                return;
            }

            _window = new MainWindow(new AcadLightningProtectorRepository());
            _window.Closed += (_, _) => _window = null;

            AcUiApp.ShowModelessWindow(_window);
        }
        catch (System.Exception ex)
        {
            _window = null;
            AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nError: " + ex.Message);
        }
    }
}
