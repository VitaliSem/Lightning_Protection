using LP.UI.ViewModels;

namespace LP.UI.Services;

/// <summary>Opens the modal dialogs of the window; replaced by a fake in the view model tests.</summary>
public interface IDialogService
{
    /// <summary>Shows the add dialog; returns true when a lightning protector was added.</summary>
    bool ShowAddLightningProtector(AddLightningProtectorViewModel viewModel);
}
