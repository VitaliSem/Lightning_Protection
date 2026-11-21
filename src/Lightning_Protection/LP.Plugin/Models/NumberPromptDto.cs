namespace LP.Plugin.Models;

public class NumberPromptDto
{
    public string Message { get; set; } = string.Empty;
    public double MaxValue { get; set; } = double.MaxValue;
    public string FieldName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double? DefaultValue { get; set; }
}
