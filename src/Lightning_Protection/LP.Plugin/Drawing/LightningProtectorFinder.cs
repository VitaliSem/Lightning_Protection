using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using LP.Core.Constants;
using LP.Core.Helpers;
using LP.Core.Models.Base;
using LP.Core.Services;
using LP.Plugin.Acad;
using LP.Plugin.Models;

namespace LP.Plugin.Drawing;

/// <summary>Locates lightning protector block references in a drawing.</summary>
public static class LightningProtectorFinder
{
    private const string NotFoundMessage = "Lightning protector #{0} not found.";
    private const string DuplicateNumberMessage = "Several lightning protectors have number #{0}; fix the numbering first.";

    /// <summary>
    /// Returns all lightning protector references in model space and layouts with their attribute values,
    /// insertion points and, for references in paper space, the name of their layout.
    /// </summary>
    public static List<LightningProtectorReferenceDto> FindAll(Transaction tr, Database db)
    {
        var protectors = new List<LightningProtectorReferenceDto>();
        ObjectId modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);

        foreach (ObjectId refId in BlockHelper.FindBlockReferences(tr, db, LightningProtectorBlockService.BlockName))
        {
            var br = (BlockReference)tr.GetObject(refId, OpenMode.ForRead);

            protectors.Add(new LightningProtectorReferenceDto
            {
                Id = refId,
                AttributeValues = BlockAttributeHelper.GetAttributeValues(tr, br),
                Position = br.Position,
                LayoutName = br.OwnerId == modelSpaceId ? null : ViewHelper.GetLayoutName(tr, br.OwnerId),
            });
        }

        return protectors;
    }

    public static List<string> GetExistingNumbers(Transaction tr, Database db) =>
        FindAll(tr, db)
            .Where(p => p.AttributeValues.ContainsKey(LightningProtectorAttributes.Number))
            .Select(p => p.AttributeValues[LightningProtectorAttributes.Number])
            .ToList();

    /// <summary>Finds the single lightning protector with the given number; fails when there is none or more than one.</summary>
    public static OperationResult<LightningProtectorReferenceDto> FindByNumber(Transaction tr, Database db, int number)
    {
        var matches = FindAll(tr, db)
            .Where(p => HasNumber(p, number))
            .ToList();

        return matches.Count switch
        {
            1 => OperationResult<LightningProtectorReferenceDto>.Ok(matches[0]),
            0 => OperationResult<LightningProtectorReferenceDto>.Fail(
                string.Format(CultureInfo.InvariantCulture, NotFoundMessage, number)),
            _ => OperationResult<LightningProtectorReferenceDto>.Fail(
                string.Format(CultureInfo.InvariantCulture, DuplicateNumberMessage, number)),
        };
    }

    private static bool HasNumber(LightningProtectorReferenceDto protector, int number) =>
        protector.AttributeValues.TryGetValue(LightningProtectorAttributes.Number, out string? value)
        && AttributeValueHelper.TryParseNumber(value, out int found)
        && found == number;
}
