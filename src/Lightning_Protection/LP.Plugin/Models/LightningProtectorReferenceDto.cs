using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace LP.Plugin.Models;

public class LightningProtectorReferenceDto
{
    public ObjectId Id { get; set; }
    public Dictionary<string, string> AttributeValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Point3d Position { get; set; }
    public string? LayoutName { get; set; }
}
