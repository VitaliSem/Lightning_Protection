using Autodesk.AutoCAD.DatabaseServices;

namespace LP.Plugin.Models;

public class LightningProtectorReferenceDto
{
    public ObjectId Id { get; set; }
    public Dictionary<string, string> AttributeValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
