using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace LP.Plugin.Models;

public class AttributeDefinitionDto
{
    public string Tag { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public Point3d Position { get; set; } = Point3d.Origin;
    public double Height { get; set; } = 2.5;
    public bool Invisible { get; set; }
    public bool Constant { get; set; }
    public AttachmentPoint Justify { get; set; } = AttachmentPoint.MiddleCenter;
    public bool LockPosition { get; set; } = true;
}
