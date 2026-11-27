using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;
using LP.Core.Services;
using LP.Plugin.Acad;
using LP.Plugin.Models;

namespace LP.Plugin.Drawing;

/// <summary>Inserts a new lightning protector block; shared by the insert command and the UI window.</summary>
public static class LightningProtectorInsert
{
    /// <summary>
    /// Validates the protector data and inserts its block reference into the current space,
    /// numbered after the existing protectors. The caller commits the transaction on success.
    /// </summary>
    public static OperationResult<LightningProtectorBlockData> Insert(Transaction tr, Database db, LightningProtectorInsertDto dto)
    {
        ObjectId btrId = LightningProtectorBlock.EnsureDefinition(tr, db);

        OperationResult<LightningProtectorBlockData> result = LightningProtectorBlockService.Create(
            new LightningProtectorBlockCreateDto
            {
                ExistingNumbers = LightningProtectorFinder.GetExistingNumbers(tr, db),
                Name = dto.Name,
                Title = dto.Title,
                Type = dto.Type,
                HeightM = dto.HeightM,
                BaseHeightM = dto.BaseHeightM,
                Description = dto.Description,
            });

        if (!result.IsSuccess)
            return result;

        BlockHelper.InsertAnnotativeBlock(tr, db, new BlockInsertDto
        {
            BlockId = btrId,
            Position = new Point3d(dto.Position.X, dto.Position.Y, dto.Position.Z),
            Layer = LightningProtectorBlockService.LayerName,
            AttributeValues = LightningProtectorBlockService.ToAttributeValues(result.Result),
            SpaceId = db.CurrentSpaceId,
            AnnotationScale = db.Cannoscale.Name,
        });

        return result;
    }
}
