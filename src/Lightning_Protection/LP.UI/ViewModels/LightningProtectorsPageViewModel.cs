using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using LP.UI.Infrastructure;

namespace LP.UI.ViewModels;

public class LightningProtectorsPageViewModel : PageViewModel
{
    private const string NotAvailableMessage = "{0} lightning protectors from this window is not available yet.";

    private LightningProtectorItemViewModel? _selectedItem;
    private string _statusMessage = "";

    public LightningProtectorsPageViewModel()
    {
        // Loading the list and the actions on it come in follow-up tasks.
        AddCommand = new RelayCommand(() => ReportNotAvailable("Adding"));
        EditCommand = new RelayCommand(() => ReportNotAvailable("Editing"), () => SelectedItem is not null);
        DeleteCommand = new RelayCommand(() => ReportNotAvailable("Deleting"), () => SelectedItem is not null);
        ToggleActiveCommand = new RelayCommand(() => ReportNotAvailable("Switching the status of"), () => SelectedItem is not null);
    }

    public override string Title => "Lightning protectors";

    public override string Subtitle => Summary;

    public override string IconData => "M8,2 V14 M2,14 L8,4 L14,14";

    public override void Refresh()
    {
    }

    public ObservableCollection<LightningProtectorItemViewModel> Items { get; } = [];

    public LightningProtectorItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value))
                return;

            OnPropertyChanged(nameof(SelectedText));
            OnPropertyChanged(nameof(IsSelectedActive));
            OnPropertyChanged(nameof(ToggleText));
        }
    }

    public string Summary =>
        string.Format(CultureInfo.InvariantCulture, "{0} total · {1} active", Items.Count, Items.Count(i => i.IsActive));

    public string SelectedText => SelectedItem?.DisplayName ?? "none";

    public bool IsSelectedActive => SelectedItem?.IsActive ?? false;

    public string ToggleText => SelectedItem is null ? "Status" : SelectedItem.StatusText;

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
                OnPropertyChanged(nameof(HasStatus));
        }
    }

    public bool HasStatus => StatusMessage.Length > 0;

    public ICommand AddCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand ToggleActiveCommand { get; }

    private void ReportNotAvailable(string action) =>
        StatusMessage = string.Format(CultureInfo.InvariantCulture, NotAvailableMessage, action);
}
