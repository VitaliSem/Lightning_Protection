using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace LP.Plugin.Models;

public class BlockDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public Point3d Origin { get; set; } = Point3d.Origin;
    public IEnumerable<Entity> Entities { get; set; } = [];
    public string? Description { get; set; }
    public bool Annotative { get; set; }
}
