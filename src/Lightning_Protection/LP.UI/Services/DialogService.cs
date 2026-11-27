using System.Windows;
using LP.UI.Views;
using LP.UI.ViewModels;

namespace LP.UI.Services;

public class DialogService(Window owner) : IDialogService
{
    private readonly Window _owner = owner ?? throw new ArgumentNullException(nameof(owner));

    /// <summary>
    /// Shows the add dialog modally. Picking the position closes the dialog and hides the owner window,
    /// so the drawing is free for the pick, then reopens the dialog with the entered values.
    /// </summary>
    public bool ShowAddLightningProtector(AddLightningProtectorViewModel viewModel)
    {
        while (true)
        {
            var dialog = new AddLightningProtectorDialog(viewModel) { Owner = _owner };

            if (dialog.ShowDialog() == true)
                return true;

            if (!viewModel.IsPickRequested)
                return false;

            _owner.Hide();

            try
            {
                viewModel.PickPosition();
            }
            finally
            {
                _owner.Show();
                _owner.Activate();
            }
        }
    }
}
