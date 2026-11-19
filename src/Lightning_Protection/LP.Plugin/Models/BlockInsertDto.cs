using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace LP.Plugin.Models;

public class BlockInsertDto
{
    public ObjectId BlockId { get; set; }
    public Point3d Position { get; set; } = Point3d.Origin;
    public double Scale { get; set; } = 1.0;
    public double Rotation { get; set; }
    public string? Layer { get; set; }
    public IDictionary<string, string>? AttributeValues { get; set; }
    public ObjectId SpaceId { get; set; } = ObjectId.Null;
    public string? AnnotationScale { get; set; }
}
