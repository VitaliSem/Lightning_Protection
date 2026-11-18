---
name: autocad-api
description: AutoCAD .NET API patterns for the Lightning_Protection plugin — layers, primitives (Line/Circle/Polyline with bulge arcs), DBText/MText and text styles, blocks with attributes, annotative blocks and annotation scales (CANNOSCALE). Use whenever writing or reviewing code in LP.Plugin (net48 + net8.0) or LP.UI that touches AutoCAD (Database, Transaction, Entity, BlockTableRecord, LayerTable, AnnotationScale), or when drawing an LP.Core ProtectionArea (single rod, single wire, double rod) into a drawing.
---

# AutoCAD .NET API for Lightning_Protection

Working rules and ready-to-copy helpers for the AutoCAD side of the solution
`src/Lightning_Protection/Lightning_Protection.slnx`. The logic comes from the consolidated guides in
`D:\Vitali.Siamashka\Autodesk` (`layers-general-guide.md`, `primitives-guide.md`, `text-guide.md`,
`blocks-guide.md`, `annotation-scales-guide.md`), adapted to this project.

## Solution layout (what may reference AutoCAD)

| Project | Target | AutoCAD | Notes |
| --- | --- | --- | --- |
| `LP.Core` | `netstandard2.0` | **never** | Calculations, `ProtectionArea`, `OperationResult<T>`, `IAcadDocument`/`IAcadTransaction`. C# 7.3 style, no nullable, no implicit usings. |
| `LP.Core.Tests` | `net8.0`, xUnit | no | Tests for LP.Core only. |
| `LP.Plugin` | `net8.0;net48` (one SDK project) | yes | `net48` → `AutoCAD.NET` 24.3.0 (AutoCAD 2024); `net8.0` → `AutoCAD.NET.Core` 25.0.0 (AutoCAD 2025+). `ExcludeAssets=runtime`. **LangVersion 12, ImplicitUsings, Nullable enabled.** All code from this skill goes here. |
| `LP.UI` | .NET Framework 4.8, WinForms | via the plugin | Only `net48`; cannot be referenced by the `net8.0` build as-is. |

Recommended folders inside `LP.Plugin`: `Acad/` (helpers from `examples/`), `Drawing/` (`ProtectionAreaDrawer`),
`Commands/` (`[CommandMethod]` classes). `LP.Plugin.csproj` currently has **no `ProjectReference` to LP.Core** —
add `<ProjectReference Include="..\LP.Core\LP.Core.csproj" />` before using the LP.Core-aware examples.

LP.Core namespaces: `LP.Core.Models.Base`, `LP.Core.Enums`, `LP.Core.Interfaces`, `LP.Core.Services.SN_4_04_03`,
`LP.Core.Models.LightningProtectors.SingleRod | SingleWire | DoubleRod | DoubleWire`.

## Writing code that compiles for both targets

- C# 12 syntax is fine on both (file-scoped namespaces, target-typed `new()`, pattern matching, switch expressions,
  collection expressions, nullable annotations). Avoid features needing runtime types missing in net48: `init`/records
  (no `IsExternalInit`), `required`, `Index`/`Range` (`^1`, `..`), default interface members.
- Avoid BCL APIs absent in net48: `Math.Clamp`, `ArgumentNullException.ThrowIfNull`, `string.Contains(char)`,
  `string.Contains(string, StringComparison)`, `Dictionary.GetValueOrDefault`, `HashSet(capacity)`, `KeyValuePair` deconstruction,
  and the nullable attributes (`[NotNullWhen]`, `[MaybeNull]` … — not in net48 without a polyfill).
- **`Application`: use `Autodesk.AutoCAD.ApplicationServices.Core.Application`** (in AcCoreMgd). The full
  `Autodesk.AutoCAD.ApplicationServices.Application` lives in AcMgd, which the `net8.0` target does not reference
  (`AutoCAD.NET.Core` only). Alias: `using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;`.
  `Document`, `DocumentCollection`, `Editor`, `CommandMethod`, `CommandClass` are all in AcCoreMgd.
- Version-specific code: `#if NET8_0_OR_GREATER` (AutoCAD 2025+) / `#else` (AutoCAD 2024).
- Nullable: lookups return `T?` / `ObjectId.Null`; never pass `null!` to AutoCAD setters; `ObjectId` is a struct.

## Non-negotiable rules (all guides agree)

1. **Every read/write happens inside a `Transaction`**, and `tr.Commit()` is called explicitly — an uncommitted
   transaction rolls everything back.
2. **Open for read, upgrade for write.** Symbol tables `ForRead`, then `UpgradeOpen()` only to add a record.
3. **New objects:** `space.AppendEntity(ent)` / `table.Add(rec)` **then** `tr.AddNewlyCreatedDBObject(x, true)`.
   An entity created with `new` but never added must be disposed.
4. **`table.Has(name)` before `table[name]`** (else `eKeyNotFound`); names are case-insensitive; validate new names
   with `SymbolUtilityServices.ValidateSymbolName(name, false)`.
5. **Layer / linetype / style must exist** before assigning `Entity.Layer`, `.Linetype`, `.TextStyleId`.
6. **Lock the document** outside a `[CommandMethod]` (LP.UI forms, palettes, events): `using (doc.LockDocument())`.
7. **Locked layers:** `tr.GetObject(id, OpenMode.ForWrite, false, true)` or catch `ErrorStatus.OnLockedLayer`.
8. Alias `using AcException = Autodesk.AutoCAD.Runtime.Exception;` (implicit usings bring `System` in).
   `FontDescriptor` needs `using FontDescriptor = Autodesk.AutoCAD.GraphicsInterface.FontDescriptor;`
   (a full `using ...GraphicsInterface;` makes `Polyline` ambiguous).
9. **Angles are radians**; format numbers with `CultureInfo.InvariantCulture`.
10. **No exceptions across boundaries:** commands catch and print via `ed.WriteMessage`; LP.Core results are
    checked with `OperationResult.IsSuccess` / `Message`.

## Cheat sheet

| Task | API | Details |
| --- | --- | --- |
| Layer exists / id | `LayerTable.Has(name)` / `lt[name]` | [layers](references/layers.md) |
| Create layer | `new LayerTableRecord{Name, Color}` → `lt.Add` → `AddNewlyCreatedDBObject` | [layers](references/layers.md) |
| Current layer | `db.Clayer = id` (frozen layer cannot be current) | [layers](references/layers.md) |
| Freeze / no-plot / lock / off | `IsFrozen` / `IsPlottable` / `IsLocked` / `IsOff` | [layers](references/layers.md) |
| Delete layer | not 0/Defpoints/current → `db.Purge(ids)` → `Erase(true)` | [layers](references/layers.md) |
| Line / Circle | `new Line(p1,p2)` / `new Circle(c, Vector3d.ZAxis, r)` | [primitives](references/primitives.md) |
| Polyline | `new Polyline()` + `AddVertexAt(i, pt2d, bulge, 0, 0)`; `Closed = true` | [primitives](references/primitives.md) |
| Arc segment | bulge = `tan(θ/4)` on the segment's **start** vertex | [primitives](references/primitives.md) |
| Move/rotate/scale | `Entity.TransformBy(Matrix3d.*)` | [primitives](references/primitives.md) |
| Single-line text | `DBText` (`Position`, `Height`, `AlignmentPoint` + `AdjustAlignment`) | [text](references/text.md) |
| Multiline text | `MText` (`Location`, `TextHeight`, `Width`, `Attachment`, `\P`) | [text](references/text.md) |
| Text style | `TextStyleTableRecord` (`Font` / `FileName`), `db.Textstyle` | [text](references/text.md) |
| Block definition | `BlockTableRecord{Name, Origin}` → `bt.Add` → `AppendEntity` | [blocks](references/blocks.md) |
| Insert block | `new BlockReference(pos, btrId)` **+ attribute references manually** | [blocks](references/blocks.md) |
| Annotative block | `btr.Annotative = AnnotativeStates.True` + scale on every reference | [blocks](references/blocks.md) |
| Current annotation scale | `db.Cannoscale` / `CANNOSCALE` | [scales](references/annotation-scales.md) |
| Scale list | `db.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES")` | [scales](references/annotation-scales.md) |
| Draw LP.Core result | `ProtectionAreaDrawer.Draw(tr, db, area, layer)` | [protection areas](references/protection-areas.md) |

## How to use this skill

1. Read the reference for the area you touch — each lists rules, the canonical snippet and the error table.
2. Prefer the helpers in `examples/` over raw API code. They target `LP.Plugin` directly (namespace `LP.Plugin.Acad`,
   C# 12, nullable-clean, rely on implicit usings) and compile for both targets:
   - `LayerHelper.cs` — find / create / delete / current / freeze / plot / lock, `LpLayers` constants.
   - `PrimitivesHelper.cs` — lines, circles, polylines, bulge math, transforms.
   - `TextHelper.cs` — DBText, MText, annotative labels, text styles, bulk edit.
   - `BlockHelper.cs` — definitions, references, attributes, annotative blocks.
   - `ScaleHelper.cs` — annotation scales and scale contexts on objects.
   - `ProtectionAreaDrawer.cs` — `ProtectionArea` → circles / closed polyline (all current LP.Core shapes).
   - `AcadDocumentAdapter.cs` — concrete `IAcadDocument` / `IAcadTransaction`.
   - `LpDrawCommands.cs` — `LP_ROD`, `LP_WIRE`, `LP_DROD` end-to-end commands.
3. Helpers take `(Transaction tr, Database db, ...)` to compose many operations in **one** transaction (preferred in
   commands: one undo step), plus self-contained wrappers for one-off calls (AutoCAD nests transactions).

## Project conventions for drawings

- Layers (constants in `LpLayers`): `LP-ZONE` (zones, color 1), `LP-PROTECTOR` (rods/wires, 5), `LP-ANNO` (labels, 7),
  `LP-AUX` (construction, non-plottable). Create on demand with `LayerHelper.EnsureProjectLayers`.
- **Model units are metres** (LP.Core works in metres). Zones are drawn 1:1 and are never annotative; markers and
  labels are annotative with sizes in **paper millimetres**.
- Z from `Point3D` is ignored in plan view.
- `ProtectionArea` semantics and the drawer's interpretation rules: see `references/protection-areas.md`.

## Common errors (quick lookup)

| Error | Cause / fix |
| --- | --- |
| `eKeyNotFound` | layer/block/style/linetype missing — `Has()` first, create it |
| `eDuplicateRecordName` | record with this (case-insensitive) name already exists |
| `eWasOpenForRead` | `UpgradeOpen()` or open `ForWrite` |
| `eOnLockedLayer` | unlock layer or `GetObject(id, ForWrite, false, true)` |
| `eWasErased` | check `id.IsErased` |
| `eLockViolation` | modifying from modeless UI without `doc.LockDocument()` |
| `CS0234 ... Application` on net8.0 | use `ApplicationServices.Core.Application` |
| Changes vanish | transaction not committed |
| Block inserted without attributes | `AttributeReference`s must be created from definitions |
| Annotative object at wrong size | no context for the current scale — add one |
