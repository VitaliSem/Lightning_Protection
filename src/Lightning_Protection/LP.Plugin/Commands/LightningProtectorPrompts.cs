using LP.Core.Services;
using LP.Plugin.Acad;
using LP.Plugin.Models;

namespace LP.Plugin.Commands;

public static class LightningProtectorPrompts
{
    public const string InsertionPoint = "\nInsertion point: ";
    public const string BaseHeight = "\nBase height, m <none>: ";
    public const string Number = "\nLightning protector number: ";
    public const string IsActive = "\nIs active: ";

    public const string Updated = "\nLightning protector #{0} updated.";
    public const string Activated = "\nLightning protector #{0} is now active.";
    public const string Deactivated = "\nLightning protector #{0} is now inactive.";
    public const string Deleted = "\nLightning protector #{0} deleted.";
    public const string DeleteCancelled = "\nLightning protector #{0} was not deleted.";

    private const string ClearHint = " (" + PromptHelper.ClearTextInput + " to clear)";

    public static readonly TextPromptDto Name = new()
    {
        Message = "\nName <none>: ",
        MaxLength = LightningProtectorBlockService.MaxNameLength,
        FieldName = "Name",
    };

    public static readonly TextPromptDto Title = new()
    {
        Message = "\nTitle <none>: ",
        MaxLength = LightningProtectorBlockService.MaxTitleLength,
        FieldName = "Title",
    };

    public static readonly TextPromptDto Type = new()
    {
        Message = "\nType <none>: ",
        MaxLength = LightningProtectorBlockService.MaxTypeLength,
        FieldName = "Type",
    };

    public static readonly NumberPromptDto Height = new()
    {
        Message = "\nHeight, m: ",
        MaxValue = LightningProtectorBlockService.MaxProtectorHeight,
        FieldName = "Height",
        Unit = "m",
    };

    public static readonly TextPromptDto Description = new()
    {
        Message = "\nDescription <none>: ",
        MaxLength = LightningProtectorBlockService.MaxDescriptionLength,
        FieldName = "Description",
    };

    public static TextPromptDto EditName(string current) => EditText(Name, "\nName", current);

    public static TextPromptDto EditTitle(string current) => EditText(Title, "\nTitle", current);

    public static TextPromptDto EditType(string current) => EditText(Type, "\nType", current);

    public static TextPromptDto EditDescription(string current) => EditText(Description, "\nDescription", current);

    public static NumberPromptDto EditHeight(double current) => new()
    {
        Message = "\nHeight, m: ",
        MaxValue = Height.MaxValue,
        FieldName = Height.FieldName,
        Unit = Height.Unit,
        DefaultValue = current,
    };

    public static NumberPromptDto EditBaseHeight(double? current) => new()
    {
        Message = (current.HasValue ? "\nBase height, m" + ClearHint : "\nBase height, m") + ": ",
        FieldName = "Base height",
        Unit = "m",
        DefaultValue = current,
    };

    public static string ConfirmDelete(int number, string name) =>
        string.IsNullOrWhiteSpace(name)
            ? $"\nDelete lightning protector #{number}? "
            : $"\nDelete lightning protector #{number} ({name})? ";

    private static TextPromptDto EditText(TextPromptDto source, string label, string current) => new()
    {
        Message = (string.IsNullOrEmpty(current) ? label : label + ClearHint) + ": ",
        MaxLength = source.MaxLength,
        FieldName = source.FieldName,
        DefaultValue = current,
    };
}
