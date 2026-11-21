using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using LP.Core.Constants;
using LP.Core.Services;
using LP.Plugin.Acad;
using LP.Plugin.Models;

namespace LP.Plugin.Drawing;

/// <summary>
/// Annotative lightning protector block: a Ø10 circle crossed by two 15-unit lines (X shape) at 1:1,
/// all on the protector layer. Annotative, left-aligned attributes are stacked to the right of the center.
/// </summary>
public static class LightningProtectorBlock
{
    private const double CircleRadius = 5.0;
    private const double CrossLineLength = 15.0;
    private const double TextHeight = 2.5;
    private const double TextOffsetX = 10.0;
    private const double LineSpacing = 4.0;

    /// <summary>Returns the lightning protector block definition, creating it and its layer when missing.</summary>
    public static ObjectId EnsureDefinition(Transaction tr, Database db)
    {
        LayerHelper.EnsureLayer(tr, db, new LayerDto
        {
            Name = LightningProtectorBlockService.LayerName,
            ColorIndex = 5,
            Description = "Lightning protectors",
        });

        ObjectId id = BlockHelper.GetBlockId(tr, db, LightningProtectorBlockService.BlockName);

        if (!id.IsNull)
            return id;

        var d = CrossLineLength / 2 * Math.Cos(Math.PI / 4);

        List<Entity> geometry =
        [
            new Circle(Point3d.Origin, Vector3d.ZAxis, CircleRadius),

            new Line(new Point3d(-d, -d, 0), new Point3d(d, d, 0)),
            new Line(new Point3d(-d, d, 0), new Point3d(d, -d, 0)),

            NewAttribute(db, 1, new AttributeDefinitionDto
                { Tag = LightningProtectorAttributes.Number, Prompt = "Number", Invisible = true }),
            NewAttribute(db, 0, new AttributeDefinitionDto
                { Tag = LightningProtectorAttributes.Name, Prompt = "Name", LockPosition = false }),
            NewAttribute(db, -1, new AttributeDefinitionDto
                { Tag = LightningProtectorAttributes.HeightM, Prompt = "Height, m", LockPosition = false }),
            NewAttribute(db, -2, new AttributeDefinitionDto
                { Tag = LightningProtectorAttributes.BaseHeightM, Prompt = "Base height, m", LockPosition = false }),
            NewAttribute(db, -3, new AttributeDefinitionDto
                { Tag = LightningProtectorAttributes.Title, Prompt = "Title", Invisible = true }),
            NewAttribute(db, -4, new AttributeDefinitionDto
                { Tag = LightningProtectorAttributes.Type, Prompt = "Type", Invisible = true }),
            NewAttribute(db, -5, new AttributeDefinitionDto
                { Tag = LightningProtectorAttributes.Description, Prompt = "Description", Invisible = true }),
            NewAttribute(db, -6, new AttributeDefinitionDto
                { Tag = LightningProtectorAttributes.IsActive, Prompt = "Is active", Invisible = true, DefaultValue = "true" }),
        ];

        foreach (Entity e in geometry)
        {
            if (e is not AttributeDefinition)
                e.SetDatabaseDefaults(db);

            e.Layer = LightningProtectorBlockService.LayerName;
            e.ColorIndex = 256;
        }

        return BlockHelper.EnsureBlock(tr, db, new BlockDefinitionDto
        {
            Name = LightningProtectorBlockService.BlockName,
            Origin = Point3d.Origin,
            Entities = geometry,
            Description = "Lightning protector",
            Annotative = true,
        });
    }

    private static AttributeDefinition NewAttribute(Database db, int row, AttributeDefinitionDto attribute)
    {
        attribute.Position = new Point3d(TextOffsetX, row * LineSpacing, 0);
        attribute.Height = TextHeight;
        attribute.Justify = AttachmentPoint.MiddleLeft;

        return BlockAttributeHelper.NewAttributeDefinition(db, attribute);
    }
}
