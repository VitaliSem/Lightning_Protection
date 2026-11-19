namespace LP.Plugin.Models;

public class LayerDto
{
    public string Name { get; set; } = string.Empty;
    public short ColorIndex { get; set; } = 7;
    public bool IsPlottable { get; set; } = true;
    public string? Description { get; set; }
}
