# LP.Core

Project-specific guidance for `LP.Core`. Cross-project rules (build/test commands, solution layout, shared conventions, implementation status) are in the repository root `CLAUDE.md`.

## Language and target

- `netstandard2.0` — **no nullable reference types and no implicit usings**. Write explicit `using` directives and C# 7.3-compatible code.
- Zero external dependencies; never references AutoCAD. AutoCAD access goes through the abstractions in `Interfaces/`.

## Folder layout

- `Models/` — domain models (see below).
- `Services/` — calculation and block-data services.
- `Interfaces/` — service contract and AutoCAD abstractions.
- `Constants/` — names that must not be hard-coded elsewhere: `LightningProtectorAttributes` (attribute tags, height text prefixes, editable-tag list), `ViewFocusConstants` (fill fraction and minimum view height used when focusing the view).
- `Helpers/` — generic helpers: `AttributeValueHelper` (`Clean`, `GetValue`, `TryParseNumber`).
- `Enums/` — e.g. `LightningProtectionReliability`.

## Domain Models

Base models in `Models/Base/` (namespace `LP.Core.Models.Base`):
- `Point3D` (X, Y, Z coordinates)
- `Polyline3D` (list of Point3D with IsClosed flag)
- `ProtectionPoint` (Point3D + optional Radius + IsLastPoint flag)
- `ProtectionArea` (list of ProtectionPoint)
- `OperationResult<T>` — generic result wrapper with IsSuccess/Message/Exception (`Ok(...)` / `Fail(message, exception)`)
- `ViewFocusDto` (extents min/max + view aspect ratio) and `ViewWindow` (centre, width, height) — input and output of `ViewFocusService`

Protector models in `Models/LightningProtectors/<Type>/` (namespace `LP.Core.Models.LightningProtectors.<Type>`):
- `SingleRod/SingleRodLightningProtector` — one position + height
- `SingleRod/SingleRodProtectionArea` — center point + radius at height hx
- `SingleWire/SingleWireLightningProtector` — two pin points with heights
- `SingleWire/SingleWireProtectionArea` — two pin points with radii
- `DoubleRod/DoubleRodLightningProtector` — two SingleRodLightningProtectors (composite)
- `DoubleRod/DoubleRodProtectionArea` — both rod areas + zone parameters (h0, r0, L, Lmax, Lc, hc, IsCombined, rx, lx, rcx, lx1, lx2)
- `DoubleWire/DoubleWireLightningProtector` — two SingleWireLightningProtectors (composite)
- `Block/` — lightning protector block data: `LightningProtectorBlockData`, `LightningProtectorBlockCreateDto`, `LightningProtectorBlockUpdateDto`, `LightningProtectorListEntry` (block data with its insertion point and, for paper space, its layout name; one row of the UI table)

`ProtectionArea` convention: `Radius == null` → straight segment starts at the point; `Radius` set → arc of that radius starts at the point; `IsLastPoint == true` closes the outline; a single point with a radius is a circle. The double-rod non-combined result is two points with radii = two separate circles (point = centre).

## Calculation Pipeline (SN 4.04.03)

All calculation services follow the same pattern:

1. Service exposes a static `Calculate()` method that returns `OperationResult<T>`; bodies are wrapped in try/catch and exceptions are converted to `Fail` — keep this convention
2. Protected cone model: cone height `h0`, base radius `r0`, protection radius at height hx: `rx = r0 * (h0 - hx) / h0`
3. Three lightning protection reliability levels: 0.900, 0.990, 0.999 (`LightningProtectionReliability.P0_900 / P0_990 / P0_999`)
4. Height-dependent coefficients for three ranges: 0-30m, 30-100m, 100-150m (max 150m). The coefficients come directly from the standard's tables — change them only against the standard
5. Validation: protector height `> 0` and `<= 150` m; `hx >= 0` and `hx < h` (for double rod: less than both heights)

Service implementations (`Services/SN_4_04_03/`):
- `SingleRodProtectionService` — Table 10.1: single rod (fully implemented). `CalculateConeHeight` / `CalculateConeBaseRadius` are reused by the double-rod service.
- `SingleWireProtectionService` — Table 10.2: single wire (fully implemented). Also has `GetProtectionArea()` that computes external tangent lines between two circles.
- `DoubleRodProtectionService` — double rod (implemented). `Calculate()` derives averaged `h0`/`r0`, `Lmax`, `Lc`, `hc` and decides combined vs separate zones; `GetProtectionArea()` returns two circles (L > Lmax) or a 6-point closed outline (separate `hx >= hc` and `hx < hc` builders).
- `LightningProtectionService` — orchestrator implementing `ILightningProtectionService`. Wraps the static services; the double wire method throws `NotImplementedException`.

Other services (`Services/`):
- `GeometryService` — static 2D helpers: Get2DDistance, GetAngleToHorizontal, GetTangentDeviationAngle (Z is ignored).
- `ViewFocusService` — `CalculateFocusWindow`: a view window centred on given extents, sized so they fill a fixed share of the view at the view's aspect ratio (validated `OperationResult<ViewWindow>`).
- `LightningProtectorBlockService` — data rules of the lightning protector block: block/layer names, validation limits, numbering (`GetNextNumber`), `Create` / `Update` (validated `OperationResult<LightningProtectorBlockData>`), and conversion to/from attribute values (`ToAttributeValues` / `FromAttributeValues`).

## Key Interfaces (`Interfaces/`)

- `ILightningProtectionService` — main service contract (single rod, single wire, double rod, double wire)
- `ILightningProtectorRepository` — source of lightning protectors for the UI: the source name, `GetAll` (protectors ordered by number) and `Focus` (show one protector by number; the message describes the outcome); changing protectors comes in a follow-up task. Implemented by LP.Plugin for the active drawing and by LP.UI.App without a drawing
- `IAcadDocument` — document abstraction (Name, ExecuteInTransaction, GetModelSpace, Regen)
- `IAcadTransaction` — transaction abstraction (GetObject, AddEntity, IDisposable)

## Tests

Unit tests are in `LP.Core.Tests` (`net8.0`, xUnit, implicit usings and nullable), mirroring this project's folders (`Services/`, `Services/SN_4_04_03/`, `Helpers/`).
