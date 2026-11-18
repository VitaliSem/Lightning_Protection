# Annotation scales (Model space)

Source: `annotation-scales-guide.md`. Full code: `../examples/ScaleHelper.cs`.

"Scale" = an entry of the drawing's scale list (`1:1`, `1:100`, `1:500`…) and the **current annotation scale**
(`db.Cannoscale` / `CANNOSCALE`). It sizes **annotative** objects only. It is **not** geometry scaling
(`Matrix3d.Scaling`), view zoom, `LTSCALE` or `DIMSCALE`. Ordinary geometry (the protection zone itself) is
unaffected.

| Task | API |
| --- | --- |
| Current scale | `db.Cannoscale` → `AnnotationScale` (`Name`, `PaperUnits`, `DrawingUnits`, `Scale`) |
| Current name | `(string)AcApp.GetSystemVariable("CANNOSCALE")` (`AcApp` = `ApplicationServices.Core.Application`) |
| All scales | `db.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES")` |
| Exists / get | `occ.HasContext(name)` / `occ.GetContext(name) as AnnotationScale` |
| Create | `new AnnotationScale { Name, PaperUnits, DrawingUnits }` → `occ.AddContext(scale)` |
| Set current | `db.Cannoscale = scale` (must already be in the list) |
| Delete | `occ.RemoveContext(name)` (not the current one; wrap in try/catch) |

## Rules

- Scales are object contexts, not regular DB objects — the list can be read/changed without a transaction
  (but still lock the document outside commands).
- A scale is **paper units : drawing units**. Name is just a label; keep it consistent (`"1:500"` ↔ 1 / 500).
  Names must be unique. Build names with `CultureInfo.InvariantCulture` (`"0.######"` format).
- Every template has a default list — `HasContext` before create/delete. The same ratio may exist under
  another name (`"1:50"` vs `"1/50"`) → `FindScaleByRatio` (compare `PaperUnits / DrawingUnits` with tolerance).
- The current scale belongs to the active space: check `db.TileMode == true` (Model tab). Layout viewports have
  their own scale (`Viewport.CustomScale` / viewport annotation scale).
- A newly created scale is not current until you set it.

## Units in this project

LP.Core works in **metres**. If model units are metres and the sheet is in millimetres, a site plan "1:500"
means 1 mm on paper = 0.5 m in the model → `PaperUnits = 1`, `DrawingUnits = 0.5`. If the drawing is in
millimetres (1 unit = 1 mm) use `PaperUnits = 1`, `DrawingUnits = 500` and convert LP.Core radii ×1000.
Decide once per drawing template (check `INSUNITS` / `db.Insunits`) and keep the conversion in one place.

## Patterns

- `SetCurrentScale(db, paper, drawing)` — find by ratio, create if missing, make current, return name.
- `SwitchScaleBy(db, ±1)` — next/previous in the list with wrap-around.
- `RunAtScale(db, name, action)` — temporarily switch, run, restore in `finally`. Use it when a routine must
  create annotative objects at a given scale without leaving the drawing changed.
- `RemoveScale(db, name, fallback)` — switches to `fallback` first if the scale is current.

Removing a scale from the list does not delete geometry; annotative objects keep their stored representations
(`SCALELISTEDIT` / `-SCALELISTEDIT Reset` cleans up).

## Errors

duplicate name · `GetContext` misses because of the name format → find by ratio · setting a non-existent scale
as current · removing the current scale · zero/negative units · wrong space (layout) · unit confusion
(mm vs m vs inch: `1/8" = 1'-0"` → 0.125 : 12) · no visible effect on non-annotative geometry ·
culture-specific decimal comma · document locking.
