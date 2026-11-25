# Blocks, attributes, annotative blocks

Source: `blocks-guide.md`. Full code: `../examples/BlockHelper.cs`.

| Term | Class | Meaning |
| --- | --- | --- |
| Block table | `BlockTable` | all definitions incl. `*Model_Space`, layouts, anonymous `*U…`/`*D…` |
| Definition | `BlockTableRecord` | geometry + attribute definitions, stored once |
| Reference | `BlockReference` | an insert: position, scale, rotation, layer, attribute values |
| Attribute definition | `AttributeDefinition` | field in the definition: `Tag`, `Prompt`, default `TextString` |
| Attribute reference | `AttributeReference` | value of that field in one insert |

## Rules

- `BlockTable` `ForRead` → `UpgradeOpen()` only to add. Validate names with `ValidateSymbolName`.
- `Has(name)` also matches layouts and anonymous blocks — for "real" user blocks also check
  `!btr.IsLayout && !btr.IsAnonymous` (and skip `IsFromExternalReference` / `IsFromOverlayReference`).
- **Definition ≠ reference.** Editing the definition changes every insert.
- **A new `BlockReference` has no attributes** — create `AttributeReference`s from the definitions yourself.

## Create a definition

```csharp
BlockTableRecord btr = new BlockTableRecord();
btr.Name = name;
btr.Origin = Point3d.Origin;          // base point = insertion point
bt.UpgradeOpen();
ObjectId btrId = bt.Add(btr);
tr.AddNewlyCreatedDBObject(btr, true);
foreach (Entity e in geometry) { btr.AppendEntity(e); tr.AddNewlyCreatedDBObject(e, true); }
```

Put block geometry on layer `"0"` with `ColorIndex = 0` (ByBlock) so inserts take the layer/color of the
reference — the standard for adaptable symbols (LP rod/wire markers).

Import from a DWG if missing: `using (var src = new Database(false, true)) { src.ReadDwgFile(path,
FileOpenMode.OpenForReadAndAllShare, true, ""); db.Insert(name, src, true); }` — re-inserting the same way
redefines an existing block.

## Edit a definition

Open `ForWrite`, add/change/erase entities, then for every reference
`btr.GetBlockReferenceIds(false, true)` → `br.RecordGraphicsModified(true)` (open with
`forceOpenOnLockedLayer = true`). Also: `Origin`, `Name` (rename is safe — references follow), `Comments`,
`Explodable`.

## Delete a definition

Refuse for `IsLayout` and xrefs (detach instead). Erase all references
(`GetBlockReferenceIds(true, false)`), then `db.Purge(ids)`; if the id is still there → `Erase(true)` and commit.
If Purge refuses (nested in another block), **don't commit** — the aborted transaction restores the erased
references. `PurgeUnusedBlocks` = PURGE for blocks.

## Insert a reference

```csharp
BlockReference br = new BlockReference(position, btrId);
br.ScaleFactors = new Scale3d(scale);
br.Rotation = rotation;                       // radians
space.AppendEntity(br); tr.AddNewlyCreatedDBObject(br, true);

foreach (ObjectId id in btr)                  // only if btr.HasAttributeDefinitions
{
    AttributeDefinition ad = tr.GetObject(id, OpenMode.ForRead) as AttributeDefinition;
    if (ad == null || ad.Constant) continue;  // constant attributes live only in the definition
    AttributeReference ar = new AttributeReference();
    ar.SetAttributeFromBlock(ad, br.BlockTransform);
    ar.Position = ad.Position.TransformBy(br.BlockTransform);
    ar.TextString = valueOrDefault;
    if (ar.IsMTextAttribute) ar.UpdateMTextAttribute();
    br.AttributeCollection.AppendAttribute(ar);
    tr.AddNewlyCreatedDBObject(ar, true);
}
```

Model space regardless of active tab: `SymbolUtilityServices.GetBlockModelSpaceId(db)`.

## Find / edit / delete references

- **Dynamic blocks** reference anonymous definitions: real name via
  `br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord` (`GetEffectiveBlockName`).
- Scan layouts cheaply: `entId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(BlockReference)))`
  before opening.
- Edit: `Position`, `Rotation`, `ScaleFactors`, `Layer`, `ColorIndex`, `TransformBy`. Switching
  `br.BlockTableRecord` keeps old attributes → run `SyncAttributes`.
- Erasing a reference erases its attribute references.

## Attributes

- Tag: no spaces or `!`, stored upper-case. Modes: `Invisible`, `Constant`, `Preset`, `Verifiable`,
  `LockPositionInBlock`. Centered text: `ad.Justify = AttachmentPoint.MiddleCenter; ad.AlignmentPoint = p;
  ad.AdjustAlignment(db)`.
- Read: iterate `br.AttributeCollection` → `ar.Tag` / `ar.TextString` (case-insensitive dictionary).
- Write: open `ForWrite`, set `TextString`, `UpdateMTextAttribute()` for MText attributes.
- Changing a definition (prompt, default, height, modes) affects new inserts; existing ones need
  **`SyncAttributes`** (ATTSYNC equivalent: removes obsolete, adds missing, re-applies properties, keeps values,
  resets positions). Renaming a **tag** loses values (matching is by tag).
- Delete from definition + sync → gone everywhere; delete from one reference → returns on the next sync.

## Annotative blocks

1. Definition: `btr.BlockScaling = BlockScaling.Uniform; btr.Annotative = AnnotativeStates.True;`
   Draw its geometry in **paper units** (mm on the sheet).
2. Every reference needs ≥ 1 annotation scale context: `br.AddContext(occ.GetContext("1:500"))`;
   check with `br.HasContext(ctx)`, remove with `br.RemoveContext(ctx)` (never the last one). Reference
   `ScaleFactors` stays 1 — size comes from the scale. Non-uniform scale is not allowed.
3. Scales come from `db.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES")` and must exist
   (see `annotation-scales.md`). To list a reference's scales, iterate the collection and test `HasContext`.
4. Attribute heights in an annotative block are paper heights and scale with the block. Make the attribute
   definitions annotative too (`ad.Annotative = AnnotativeStates.True`, after `AddNewlyCreatedDBObject`), attach the
   scale to the reference **before** building the `AttributeReference`s and give each annotative attribute
   reference the same scale — `BlockHelper.InsertBlock(..., annotationScale: name)` does it in that order.

> The guide uses `ObjectContexts.AddContext/RemoveContext/HasContext/GetContextNames`; that static class lives in
> `Autodesk.AutoCAD.Internal` and its overloads differ between releases. The helpers here use the public
> instance methods `DBObject.AddContext / HasContext / RemoveContext`, which exist in both AutoCAD 2024 (24.3, net48)
> and AutoCAD 2025+ (25.x, net8.0-windows).

## Errors

`eDuplicateRecordName` · bad name · `eKeyNotFound` (block/layer/scale) · cannot delete (references, nested,
xref, layout) · `eOnLockedLayer` · references not redrawn (`RecordGraphicsModified`, `Regen`) · insert has no
attributes · new attribute missing in old inserts (sync) · values lost after tag rename · dynamic blocks →
anonymous names · annotative needs uniform scaling and ≥ 1 scale.
