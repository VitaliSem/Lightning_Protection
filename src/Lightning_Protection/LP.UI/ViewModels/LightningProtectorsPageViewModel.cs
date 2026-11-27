using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using LP.Core.Interfaces;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;
using LP.Core.Services;
using LP.UI.Infrastructure;
using LP.UI.Services;

namespace LP.UI.ViewModels;

public class LightningProtectorsPageViewModel : PageViewModel
{
    private const string NotAvailableMessage = "{0} lightning protectors from this window is not available yet.";
    private const string InsertedMessage = "Lightning protector #{0} added to the drawing.";

    private readonly ILightningProtectorRepository _repository;
    private readonly IDialogService _dialogService;

    private LightningProtectorItemViewModel? _selectedItem;
    private string _statusMessage = "";
    private bool _isStatusLoadError;

    public LightningProtectorsPageViewModel(ILightningProtectorRepository repository, IDialogService dialogService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        AddCommand = new RelayCommand(Add);

        // The other actions on the list come in follow-up tasks.
        EditCommand = new RelayCommand(() => ReportNotAvailable("Editing"), () => SelectedItem is not null);
        DeleteCommand = new RelayCommand(() => ReportNotAvailable("Deleting"), () => SelectedItem is not null);
        ToggleActiveCommand = new RelayCommand(() => ReportNotAvailable("Switching the status of"), () => SelectedItem is not null);
        FocusCommand = new RelayCommand<LightningProtectorItemViewModel>(Focus);
    }

    public override string Title => "Lightning protectors";

    public override string Subtitle => Summary;

    public override string IconData => "M8,2 V14 M2,14 L8,4 L14,14";

    /// <summary>
    /// Reloads the list from the repository, keeping the selected protector when it is still there.
    /// A failed load empties the list and reports its message.
    /// </summary>
    public override void Refresh()
    {
        int? selectedNumber = SelectedItem?.Number;

        OperationResult<IReadOnlyList<LightningProtectorListEntry>> loaded = _repository.GetAll();

        Items.Clear();

        if (loaded.IsSuccess)
        {
            foreach (LightningProtectorListEntry entry in loaded.Result ?? [])
                Items.Add(new LightningProtectorItemViewModel(entry));

            if (_isStatusLoadError)
                SetStatus("", isLoadError: false);
        }
        else
        {
            SetStatus(loaded.Message ?? "", isLoadError: true);
        }

        SelectedItem = Items.FirstOrDefault(i => i.Number == selectedNumber);

        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(Subtitle));
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

    public ICommand FocusCommand { get; }

    private void Add()
    {
        var nextNumber = LightningProtectorBlockService.GetNextNumber(
            Items.Select(i => i.Number.ToString(CultureInfo.InvariantCulture)));

        var dialog = new AddLightningProtectorViewModel(_repository, nextNumber);

        if (!_dialogService.ShowAddLightningProtector(dialog) || dialog.Inserted is null)
            return;

        var number = dialog.Inserted.Number;

        Refresh();
        SelectedItem = Items.FirstOrDefault(i => i.Number == number);

        if (!_isStatusLoadError)
            SetStatus(string.Format(CultureInfo.InvariantCulture, InsertedMessage, number), isLoadError: false);
    }

    private void Focus(LightningProtectorItemViewModel item)
    {
        SelectedItem = item;

        OperationResult<string> focused = _repository.Focus(item.Number);

        SetStatus(focused.Message ?? "", isLoadError: false);
    }

    private void ReportNotAvailable(string action) =>
        SetStatus(string.Format(CultureInfo.InvariantCulture, NotAvailableMessage, action), isLoadError: false);

    private void SetStatus(string message, bool isLoadError)
    {
        StatusMessage = message;
        _isStatusLoadError = isLoadError;
    }
}
