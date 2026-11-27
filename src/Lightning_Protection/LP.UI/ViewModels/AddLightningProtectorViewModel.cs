using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LP.Core.Interfaces;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;
using LP.Core.Services;
using LP.UI.Infrastructure;

namespace LP.UI.ViewModels;

/// <summary>
/// The add dialog: collects the protector values, validates them and inserts the protector through the repository.
/// Picking the position closes the dialog for the pick; the dialog service reopens it with the same view model.
/// </summary>
public class AddLightningProtectorViewModel : ValidatableViewModel
{
    private const string NumberHint = "Number #{0} is assigned automatically.";

    private readonly ILightningProtectorRepository _repository;

    private string _name = "";
    private string _title = "";
    private LightningProtectorTypeOption _selectedType = NoType;
    private string _heightText = "";
    private string _baseHeightText = "";
    private string _xText = "";
    private string _yText = "";
    private string _description = "";

    // Shown together in the box at the bottom of the form: the error shared by several fields
    // (the coordinates) and the outcome of a failed insert or pick.
    private string _formError = "";
    private string _outcomeMessage = "";

    // Errors are shown from the first confirmation on and then follow every change.
    private bool _showErrors;

    public AddLightningProtectorViewModel(ILightningProtectorRepository repository, int nextNumber)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        NumberHintText = string.Format(CultureInfo.InvariantCulture, NumberHint, nextNumber);

        AddCommand = new RelayCommand(Confirm);
        PickCommand = new RelayCommand(RequestPick);
    }

    public event EventHandler<bool>? CloseRequested;

    public string WindowTitle => "Add lightning protector";

    public string NumberHintText { get; }

    public int MaxNameLength => LightningProtectorBlockService.MaxNameLength;

    public int MaxTitleLength => LightningProtectorBlockService.MaxTitleLength;

    public int MaxDescriptionLength => LightningProtectorBlockService.MaxDescriptionLength;

    public static LightningProtectorTypeOption NoType { get; } =
        new("None", "No type: the type of the lightning protector stays empty.");

    public IReadOnlyList<LightningProtectorTypeOption> Types { get; } = [NoType, .. LightningProtectorTypeOption.All];

    public string Name { get => _name; set => SetInput(ref _name, value); }

    public string Title { get => _title; set => SetInput(ref _title, value); }

    // Optional: None is selected by default, and the selector never goes back to no selection.
    public LightningProtectorTypeOption SelectedType
    {
        get => _selectedType;
        set
        {
            if (value is not null)
                SetProperty(ref _selectedType, value);
        }
    }

    public string HeightText { get => _heightText; set => SetInput(ref _heightText, value); }

    public string BaseHeightText { get => _baseHeightText; set => SetInput(ref _baseHeightText, value); }

    public string XText { get => _xText; set => SetInput(ref _xText, value); }

    public string YText { get => _yText; set => SetInput(ref _yText, value); }

    public string Description { get => _description; set => SetInput(ref _description, value); }

    public bool IsActive => true;

    // Saving templates to the database comes in a follow-up task.
    public bool SaveAsTemplate => false;

    public bool CanSaveAsTemplate => false;

    public string ErrorMessage =>
        string.Join("\n", new[] { _formError, _outcomeMessage }.Where(m => m.Length > 0));

    public bool HasErrorMessage => ErrorMessage.Length > 0;

    public bool IsPickRequested { get; private set; }

    public LightningProtectorBlockData? Inserted { get; private set; }

    public ICommand AddCommand { get; }

    public ICommand PickCommand { get; }

    /// <summary>Picks the position through the repository; a failed pick keeps the entered coordinates and reports why.</summary>
    public void PickPosition()
    {
        IsPickRequested = false;

        OperationResult<Point3D> picked = _repository.PickPosition();

        if (!picked.IsSuccess || picked.Result is null)
        {
            SetOutcome(picked.Message ?? "");
            return;
        }

        SetOutcome("");
        XText = FormatCoordinate(picked.Result.X);
        YText = FormatCoordinate(picked.Result.Y);
    }

    /// <summary>
    /// Parses a number written with a dot or a comma as the decimal separator; false when the text is not a finite number.
    /// </summary>
    public static bool TryParseNumber(string text, out double value)
    {
        var normalized = (text ?? "").Trim().Replace(',', '.');

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
               && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private void Confirm()
    {
        _showErrors = true;
        SetOutcome("");

        if (!Validate())
            return;

        OperationResult<LightningProtectorBlockData> result = _repository.Insert(new LightningProtectorInsertDto
        {
            Position = new Point3D(ParseNumber(XText), ParseNumber(YText)),
            Name = Name,
            Title = Title,
            Type = SelectedType == NoType ? "" : SelectedType.Name,
            HeightM = ParseNumber(HeightText),
            BaseHeightM = string.IsNullOrWhiteSpace(BaseHeightText) ? null : ParseNumber(BaseHeightText),
            Description = Description,
        });

        if (!result.IsSuccess || result.Result is null)
        {
            SetOutcome(string.IsNullOrEmpty(result.Message) ? "The lightning protector was not added." : result.Message);
            return;
        }

        Inserted = result.Result;
        CloseRequested?.Invoke(this, true);
    }

    private void RequestPick()
    {
        IsPickRequested = true;
        CloseRequested?.Invoke(this, false);
    }

    /// <summary>
    /// Updates the error of every field and the shared coordinate error; true when the form is valid.
    /// The coordinates are marked on both boxes, while their message goes to the bottom of the form.
    /// </summary>
    private bool Validate()
    {
        SetError(nameof(Name), LightningProtectorBlockService.ValidateTextLength(Name, MaxNameLength, "Name"));
        SetError(nameof(Title), LightningProtectorBlockService.ValidateTextLength(Title, MaxTitleLength, "Title"));
        SetError(nameof(Description), LightningProtectorBlockService.ValidateTextLength(Description, MaxDescriptionLength, "Description"));
        SetError(nameof(HeightText), ValidateHeight(HeightText));
        SetError(nameof(BaseHeightText), ValidateOptionalNumber(BaseHeightText, "The base height"));

        var positionError = ValidatePosition(XText, YText) ?? "";
        SetError(nameof(XText), positionError);
        SetError(nameof(YText), positionError);
        SetFormError(positionError);

        return !HasErrors;
    }

    private static string? ValidateHeight(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Enter the height.";

        if (!TryParseNumber(text, out double height))
            return "The height must be a number.";

        if (height <= 0)
            return "The height must be greater than 0 m.";

        return height > LightningProtectorBlockService.MaxProtectorHeight
            ? string.Format(CultureInfo.InvariantCulture, "The height must not exceed {0} m.", LightningProtectorBlockService.MaxProtectorHeight)
            : null;
    }

    private static string? ValidateOptionalNumber(string text, string fieldName) =>
        string.IsNullOrWhiteSpace(text) || TryParseNumber(text, out _) ? null : fieldName + " must be a number.";

    private static string? ValidatePosition(string xText, string yText)
    {
        if (string.IsNullOrWhiteSpace(xText) || string.IsNullOrWhiteSpace(yText))
            return "Enter X and Y or pick the position on the drawing.";

        return TryParseNumber(xText, out _) && TryParseNumber(yText, out _) ? null : "X and Y must be numbers.";
    }

    private void SetInput(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value ?? "", propertyName) && _showErrors)
            Validate();
    }

    private void SetFormError(string error)
    {
        if (_formError == error)
            return;

        _formError = error;
        OnErrorMessageChanged();
    }

    private void SetOutcome(string message)
    {
        if (_outcomeMessage == message)
            return;

        _outcomeMessage = message;
        OnErrorMessageChanged();
    }

    private void OnErrorMessageChanged()
    {
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasErrorMessage));
    }

    private static double ParseNumber(string text) => TryParseNumber(text, out var value) ? value : 0;

    private static string FormatCoordinate(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
