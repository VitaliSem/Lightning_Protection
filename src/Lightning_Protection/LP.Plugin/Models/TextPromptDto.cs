namespace LP.Plugin.Models;

public class TextPromptDto
{
    public string Message { get; set; } = string.Empty;
    public int MaxLength { get; set; } = int.MaxValue;
    public string FieldName { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
}
