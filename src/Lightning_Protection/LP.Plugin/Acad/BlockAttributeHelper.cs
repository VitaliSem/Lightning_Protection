using Autodesk.AutoCAD.DatabaseServices;
using LP.Plugin.Models;

namespace LP.Plugin.Acad;

public static class BlockAttributeHelper
{
    public static AttributeDefinition NewAttributeDefinition(Database db, AttributeDefinitionDto attribute)
    {
        var tag = attribute.Tag;

        if (string.IsNullOrWhiteSpace(tag) || tag.Contains(' ') || tag.Contains('!'))
            throw new ArgumentException("The tag must not be empty or contain spaces or '!'.", nameof(attribute));

        var ad = new AttributeDefinition();

        ad.SetDatabaseDefaults(db);

        ad.Tag = tag.ToUpperInvariant();
        ad.Prompt = attribute.Prompt;
        ad.TextString = attribute.DefaultValue;
        ad.Position = attribute.Position;
        ad.Height = attribute.Height;
        ad.Invisible = attribute.Invisible;
        ad.Constant = attribute.Constant;
        ad.LockPositionInBlock = attribute.LockPosition;

        if (attribute.Justify == AttachmentPoint.BaseLeft) return ad;

        ad.Justify = attribute.Justify;
        ad.AlignmentPoint = attribute.Position;
        ad.AdjustAlignment(db);

        return ad;
    }

    /// <summary>Creates the attribute references of a new block reference from its block definition.</summary>
    public static void AddAttributeReferences(Transaction tr, BlockReference br, BlockInsertDto insert)
    {
        var db = br.Database;
        var values = insert.AttributeValues;
        var annotationScale = insert.AnnotationScale;

        var btr = (BlockTableRecord)tr.GetObject(insert.BlockId, OpenMode.ForRead);

        if (!btr.HasAttributeDefinitions)
            return;

        foreach (ObjectId id in btr)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition ad || ad.Constant)
                continue;

            var ar = new AttributeReference();

            ar.SetAttributeFromBlock(ad, br.BlockTransform);

            ar.Position = ad.Position.TransformBy(br.BlockTransform);
            ar.TextString = TryGetIgnoreCase(values, ad.Tag, out var value) ? value ?? "" : ad.TextString;

            if (ar.IsMTextAttribute)
                ar.UpdateMTextAttribute();
            else if (ar.Justify != AttachmentPoint.BaseLeft)
                ar.AdjustAlignment(db);

            br.AttributeCollection.AppendAttribute(ar);
            tr.AddNewlyCreatedDBObject(ar, true);

            if (annotationScale is not null && ar.Annotative == AnnotativeStates.True)
                ScaleHelper.SetSingleObjectScale(db, ar, annotationScale);
        }
    }

    public static Dictionary<string, string> GetAttributeValues(Transaction tr, BlockReference br)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (ObjectId attId in br.AttributeCollection)
        {
            if (!attId.IsErased && tr.GetObject(attId, OpenMode.ForRead) is AttributeReference ar)
                result[ar.Tag] = ar.TextString;
        }

        return result;
    }

    /// <summary>Writes the values into the matching attribute references of a block reference, keeping their placement.</summary>
    public static void SetAttributeValues(Transaction tr, BlockReference br, IDictionary<string, string> values)
    {
        var db = br.Database;

        foreach (ObjectId attId in br.AttributeCollection)
        {
            if (attId.IsErased || tr.GetObject(attId, OpenMode.ForRead) is not AttributeReference ar)
                continue;

            if (!TryGetIgnoreCase(values, ar.Tag, out var value))
                continue;

            ar.UpgradeOpen();
            ar.TextString = value ?? "";

            if (ar.IsMTextAttribute)
                ar.UpdateMTextAttribute();
            else if (ar.Justify != AttachmentPoint.BaseLeft)
                ar.AdjustAlignment(db);
        }
    }

    private static bool TryGetIgnoreCase(IDictionary<string, string>? dict, string key, out string? value)
    {
        value = null;

        if (dict is null)
            return false;

        foreach (var kv in dict)
        {
            if (!string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

            value = kv.Value;

            return true;
        }

        return false;
    }
}
