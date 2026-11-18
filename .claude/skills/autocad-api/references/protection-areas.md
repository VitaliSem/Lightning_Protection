# Drawing LP.Core protection areas

Full code: `../examples/ProtectionAreaDrawer.cs`, usage in `../examples/LpDrawCommands.cs`.

`ILightningProtectionService` (implemented by `LightningProtectionService`) returns
`OperationResult<ProtectionArea>`; a `ProtectionArea` is `List<ProtectionPoint>`; a `ProtectionPoint` is
`Point3D Point`, `double? Radius`, `bool IsLastPoint`. Coordinates and radii are **metres**, Z is ignored.

## Shapes produced by the services today

| Service call | Points | Meaning |
| --- | --- | --- |
| `GetSingleRodProtectionArea` | 1: `(center, rx, last)` | circle |
| `GetSingleWireProtectionArea`, r1 = 0 or r2 = 0 | 1: `(center, r)` | circle |
| `GetSingleWireProtectionArea` | 4: `upper1`, `upper2 (r2)`, `lower2`, `lower1 (r1, last)` | two arcs + external tangents, closed |
| `GetDoubleRodProtectionArea`, not combined (L > Lmax) | 2: `(c1, rx1)`, `(c2, rx2, last)` | **two separate circles** (points are centres) |
| `GetDoubleRodProtectionArea`, combined, `rcx == 0` | 6: `upper1 (rx1)`, `lower1`, `midLower`, `lower2 (rx2)`, `upper2`, `midUpper (last)` | semicircles at the rods + straight lines, closed |
| `GetDoubleRodProtectionArea`, combined, `rcx > 0` | 6, every vertex has a radius (`rx` / `rcx`) | arcs only, closed |
| `GetDoubleWireProtectionArea` | — | throws `NotImplementedException` — catch it in commands |

## How the drawer interprets the points

1. **Circles:** if every point has a `Radius` and there are fewer than 3 points, each point is a circle centre →
   one `Circle` per point (single rod, degenerate single wire, separated double rod).
2. **Outline:** otherwise one `Polyline`, vertices in order, `Closed = true` when the last point has
   `IsLastPoint` (never by repeating the first vertex).
3. **Straight vs arc:** `Radius == null` → the segment starting at the vertex is straight; `Radius` set → that segment
   is an arc of exactly that radius (the bulge goes on the **start** vertex).
4. **Which of the four arcs** (minor/major × CW/CCW) — the model stores no centre or direction, so:
   - if the previous segment is straight and not collinear with the chord, take the arc **tangent** to it
     (`φ` = signed angle from incoming direction to chord; CCW if `φ > 0`, major if `|φ| > 90°`). This reproduces the
     single-wire outline exactly (checked numerically) and the double-rod rod-side semicircle after a tangent line;
   - otherwise (previous segment is an arc, collinear, or none) take the **minor arc that bulges away from the
     centroid of the vertices** (for semicircles: the side away from the centroid).
5. If the radius is shorter than half the chord, `BulgeFromRadius` throws `ArgumentException` — commands catch it and
   report "cannot build the outline".

## Known gaps (fix in LP.Core, not in the drawer)

- **Arc direction is implicit.** Rule 4 is a heuristic. For the double-rod `rcx > 0` case all segments are arcs, so
  every arc is drawn convex; if the standard requires the middle `rcx` arcs to be concave, the model has to say so.
  Recommended: add an explicit, optional arc description to `ProtectionPoint` (e.g. `bool? IsClockwise` +
  `bool IsLargeArc`, or the arc centre) and let the drawer prefer it over the heuristic.
- **Multiple contours** are encoded only by "all points have radii and n < 3". A dedicated multi-contour model
  (e.g. `List<ProtectionArea>` or `ProtectionArea.Contours`) would remove the ambiguity.
- Observed with the current `DoubleRodProtectionService`: when `L ≤ Lc`, `hc = h0` gives `rc = 0`, so `rcx = 0` and
  `GetProtectionArea` takes the `hx ≥ hc` branch even though `hx < hc`. Worth checking against SN 4.04.03 together with
  the placement of `midUpper`/`midLower` (they are offset perpendicular from the rod, not from the midpoint).
  These are calculation questions — change LP.Core only against the standard and its tests.

## Drawing pattern

```csharp
OperationResult<ProtectionArea> res = service.GetDoubleRodProtectionArea(protector, reliability, hx);
if (!res.IsSuccess) { ed.WriteMessage("\n" + res.Message); return; }

using Transaction tr = db.TransactionManager.StartTransaction();
LayerHelper.EnsureProjectLayers(tr, db);
IReadOnlyList<ObjectId> ids = ProtectionAreaDrawer.Draw(tr, db, res.Result, LpLayers.Zone);
tr.Commit();
```
