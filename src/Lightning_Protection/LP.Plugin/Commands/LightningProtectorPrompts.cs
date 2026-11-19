using LP.Core.Services;
using LP.Plugin.Models;

namespace LP.Plugin.Commands;

public static class LightningProtectorPrompts
{
    public const string InsertionPoint = "\nInsertion point: ";
    public const string BaseHeight = "\nBase height, m <none>: ";

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
}
