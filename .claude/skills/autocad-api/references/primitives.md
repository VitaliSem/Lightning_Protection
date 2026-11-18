# Primitives: `Line`, `Circle`, `Polyline`

Source: `primitives-guide.md`. Full code: `../examples/PrimitivesHelper.cs`.

| Primitive | Class | DXF | Create | Key properties |
| --- | --- | --- | --- | --- |
| Line | `Line` | `LINE` | `new Line(p1, p2)` | `StartPoint`, `EndPoint`, `Length`, `Angle` |
| Circle | `Circle` | `CIRCLE` | `new Circle(center, Vector3d.ZAxis, r)` | `Center`, `Radius`, `Diameter`, `Area` |
| Lightweight polyline | `Polyline` | `LWPOLYLINE` | `new Polyline()` + `AddVertexAt` | `NumberOfVertices`, `Closed`, `Length`, `Area` |

`Polyline2d` / `Polyline3d` are the "heavy" types (DXF `POLYLINE`) — use `Polyline` for all 2D zone outlines.

## Append to a space

```csharp
BlockTableRecord ms = (BlockTableRecord)tr.GetObject(
    SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
ObjectId id = ms.AppendEntity(ent);
tr.AddNewlyCreatedDBObject(ent, true);
```

Use `db.CurrentSpaceId` instead to target whatever space is active.

## Polyline model

- A vertex = `Point2d` + **bulge** + start/end width. Vertices are in the polyline's OCS; with the default
  normal (`Vector3d.ZAxis`) that is WCS XY. Elevation is one value: `pl.Elevation`.
- **Segment `i` runs from vertex `i` to `i+1`; its bulge is stored on vertex `i`** (the start vertex).
- Open: `n-1` segments. Closed: `n` segments, the last one goes `n-1 → 0` and uses the bulge of vertex `n-1`.
- `AddVertexAt(index, pt, bulge, startW, endW)` inserts **before** `index`; append with `pl.NumberOfVertices`.
  Valid index: `0..NumberOfVertices` for `AddVertexAt`, `0..NumberOfVertices-1` for `SetPointAt`,
  `SetBulgeAt`, `RemoveVertexAt`, `GetPoint2dAt`.
- **Close with `pl.Closed = true`.** Never repeat the first point at the end — that gives an open polyline
  with a zero-length segment. `PrimitivesHelper.ClosePolyline` repairs such polylines.
- Minimum vertices: 2 (open), 3 (closed). `Area` is meaningful only when closed.
- After inserting/removing vertices indexes shift — edit from the **end towards the start**.

## Bulge (arc segments)

`bulge = tan(θ/4)`, θ = included (central) angle in radians.

| Bulge | Segment |
| --- | --- |
| `0` | straight |
| `> 0` | arc counter-clockwise from start to end vertex |
| `< 0` | clockwise |
| `±1` | semicircle |
| `tan(π/8) ≈ 0.4142` | quarter circle |

Helpers (in `PrimitivesHelper`):

- `BulgeFromAngle(θ)` → `Math.Tan(θ / 4)`.
- `BulgeFromThreePoints(start, mid, end)` — arc through a middle point, sign auto.
- `BulgeFromRadius(start, end, R, largeArc, counterClockwise)` — two points + radius. Four arcs are possible,
  so two flags are needed. Minor arc θ = `2·asin(c / 2R)`, major = `2π − θ`; requires `R ≥ c/2`.
  Rule of thumb for **minor** arcs: CCW (positive) bulges to the **right** of start→end, CW to the left.
- `TangentBulge(incomingDirection, start, end)` — arc that continues tangentially from the previous segment
  (`bulge = tan(φ/2)`, φ = signed angle from the incoming direction to the chord). Used by
  `ProtectionAreaDrawer` to draw external-tangent zone outlines without storing arc centres.
- Read back: `pl.GetSegmentType(i) == SegmentType.Arc` → `pl.GetArcSegment2dAt(i).Radius`;
  signed angle = `4 * Math.Atan(pl.GetBulgeAt(i))`.

Change an existing segment: `pl.SetBulgeAt(i, b)`; straighten with `0`; flip with `-pl.GetBulgeAt(i)`.
Append an arc to an open polyline: `SetBulgeAt(last, b)` then `AddVertexAt(last + 1, end, 0, 0, 0)`.

If you need a standalone `Arc` entity: `new Arc(center, radius, startAngle, endAngle)` (centre + angles,
always CCW) — for "two points + radius" the polyline/bulge route avoids computing the centre.

## Edit table

| Action | Code |
| --- | --- |
| Move vertex | `pl.SetPointAt(i, new Point2d(x, y))` |
| Remove vertex | `pl.RemoveVertexAt(i)` (removing a bulged vertex removes its arc) |
| Width | `pl.ConstantWidth = w` / `SetStartWidthAt(i,w)`, `SetEndWidthAt(i,w)` |
| Reverse | `pl.ReverseCurve()` (also flips the meaning of bulge signs relative to the drawing) |

## Common to all entities

- Move: `TransformBy(Matrix3d.Displacement(v))`; rotate: `Matrix3d.Rotation(a, Vector3d.ZAxis, basePt)`;
  scale: `Matrix3d.Scaling(f, basePt)`.
- Copy: `(Entity)src.Clone()` → transform → append.
- Common props: `Layer` (must exist), `ColorIndex` (256 = ByLayer, 0 = ByBlock), `Linetype` (must be loaded).
- Type check without opening: `id.ObjectClass.DxfName` (`"LINE"`, `"CIRCLE"`, `"LWPOLYLINE"`).
- Select by type: `DxfCode.Start` filter, comma list `"LINE,CIRCLE,LWPOLYLINE"`.
- `ed.GetPoint` returns **UCS** points — convert with `.TransformBy(ed.CurrentUserCoordinateSystem)`.

## Errors

`eWasOpenForRead` · `eOnLockedLayer` · `eWasErased` (check `id.IsErased`) · `eInvalidInput` (non-positive
radius, bad index) · `eKeyNotFound` (layer/linetype) · wrong arc direction (bulge sign or polyline direction) ·
bulge on the wrong vertex (must be on the **start** vertex) · undisposed entities never added to the DB.
