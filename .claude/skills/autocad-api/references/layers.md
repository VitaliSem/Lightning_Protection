# Layers (`LayerTable` / `LayerTableRecord`)

Source: `layers-general-guide.md`. Full code: `../examples/LayerHelper.cs`.

All layers live in `db.LayerTableId` (`LayerTable`); each layer is a `LayerTableRecord`.

## Rules

- Read/modify only inside a transaction. `LayerTable` → `ForRead`, `UpgradeOpen()` only to add.
  `LayerTableRecord` → `ForWrite` only to change properties.
- Names are **case-insensitive** (`Has("lp-zone")` finds `LP-ZONE`); ≤ 255 chars; validate with
  `SymbolUtilityServices.ValidateSymbolName(name, false)` (throws on `< > / \ : ; ? * | , =`).
- `lt[name]` throws `eKeyNotFound` → always `lt.Has(name)` first.
- The `LayerTable` iterator skips erased records.

## Find / create

```csharp
LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
if (!lt.Has(name))
{
    LayerTableRecord ltr = new LayerTableRecord();
    ltr.Name  = name;
    ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);   // 1..255, 7 = white/black
    // optional: ltr.IsPlottable, ltr.Description, ltr.LinetypeObjectId (linetype must be loaded: ltt.Has("CENTER"))
    lt.UpgradeOpen();
    lt.Add(ltr);
    tr.AddNewlyCreatedDBObject(ltr, true);
}
```

`LayerHelper.EnsureLayer(tr, db, "LP-ZONE", 1)` wraps this and returns the id of the new or existing layer.
Search by condition: `LayerHelper.FindLayers(db, l => l.IsFrozen)`.

## Current layer

- `db.Clayer = layerId` (or `AcApp.SetSystemVariable("CLAYER", name)` for the active doc, `AcApp` = `ApplicationServices.Core.Application`).
- A **frozen** layer cannot be made current — thaw it first. A locked/off layer can be current.
- Prefer setting `Entity.Layer` explicitly on new entities instead of switching `CLAYER`; if you do switch,
  restore the previous value.

## State

| Property | `true` means | Effect |
| --- | --- | --- |
| `IsFrozen` | frozen | not displayed / plotted / regenerated / selectable |
| `IsPlottable` | plotted | `false` = visible but not plotted (Defpoints is never plotted) |
| `IsLocked` | locked | visible, not editable; new objects can still be added |
| `IsOff` | off | not displayed / plotted, still regenerated |

- The **current** layer cannot be frozen → switch to `"0"` first.
- After freeze/thaw call `ed.Regen()`.
- VP Freeze (per-viewport) is a different mechanism from `IsFrozen`.

## Objects on a layer

- **Fast, current space only, skips frozen layers:**
  `ed.SelectAll(new SelectionFilter(new[]{ new TypedValue((int)DxfCode.LayerName, "LP-*") }))`
  (wildcards and comma lists allowed; combine with `DxfCode.Start` for type).
- **Reliable, all spaces, any layer state:** iterate every `BlockTableRecord` with `IsLayout == true`
  (optionally all definitions) and compare `ent.Layer` with `StringComparison.OrdinalIgnoreCase`.
  A `BlockReference` belongs to its own layer; entities inside a definition have their own.

## Delete

A layer cannot be deleted if it is `0` / `Defpoints`, current, xref-dependent (`xref|layer`), or referenced
(contains objects, used in block definitions). Check with `db.Purge(ids)` — it **removes** non-deletable ids
from the collection; erase only what remains. To delete with contents: erase entities first
(`GetObject(id, ForWrite, false, true)` to pass locked layers), then purge-check and erase the layer.

## Errors

`eKeyNotFound` (missing) · `eDuplicateRecordName` (exists) · `ValidateSymbolName` exception (bad name) ·
`eWasOpenForRead` · `eOnLockedLayer` · freezing current layer fails · xref layers changeable only per session ·
layer `0` cannot be renamed or deleted · use `LayerStateManager` to save/restore whole sets of states.
