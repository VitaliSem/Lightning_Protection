using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using LP.Plugin.Models;

namespace LP.Plugin.Acad;

public static class LayerHelper
{
    public static ObjectId GetLayerId(Transaction tr, Database db, string layerName)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

        return lt.Has(layerName) ? lt[layerName] : ObjectId.Null;
    }

    /// <summary>Returns an existing layer or creates it when missing.</summary>
    public static ObjectId EnsureLayer(Transaction tr, Database db, LayerDto layer)
    {
        SymbolUtilityServices.ValidateSymbolName(layer.Name, false);

        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

        if (lt.Has(layer.Name))
            return lt[layer.Name];

        var ltr = new LayerTableRecord
        {
            Name = layer.Name,
            Color = Color.FromColorIndex(ColorMethod.ByAci, layer.ColorIndex),
            IsPlottable = layer.IsPlottable,
        };

        if (layer.Description is not null)
            ltr.Description = layer.Description;

        lt.UpgradeOpen();
        ObjectId id = lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
        
        return id;
    }
}
