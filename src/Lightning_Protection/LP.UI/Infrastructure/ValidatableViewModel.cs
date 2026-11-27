using System.Collections;
using System.ComponentModel;

namespace LP.UI.Infrastructure;

/// <summary>
/// View model with per-property errors: WPF marks the control bound to a property
/// that has an error and shows the message through the control's Validation.ErrorTemplate.
/// </summary>
public abstract class ValidatableViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, string> _errors = new();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count > 0;

    public IEnumerable GetErrors(string? propertyName) =>
        propertyName is not null && _errors.TryGetValue(propertyName, out var error) ? new[] { error } : Array.Empty<string>();

    public string GetError(string propertyName) => _errors.TryGetValue(propertyName, out var error) ? error : "";

    /// <summary>Sets or, for an empty message, clears the error of a property and notifies only when it changes.</summary>
    protected void SetError(string propertyName, string? error)
    {
        if (string.IsNullOrEmpty(error))
        {
            if (!_errors.Remove(propertyName))
                return;
        }
        else
        {
            if (_errors.TryGetValue(propertyName, out var current) && current == error)
                return;

            _errors[propertyName] = error!;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }
}
