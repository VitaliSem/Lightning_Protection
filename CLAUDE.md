# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

All commands should be run from the `src/Lightning_Protection/` directory where the solution file lives.

```bash
cd src/Lightning_Protection

# Build the entire solution (LP.Plugin builds for both net48 and net8.0;
# LP.UI is a legacy .NET Framework csproj and needs Windows / MSBuild)
dotnet build Lightning_Protection.slnx

# Build only the core library (works cross-platform, no AutoCAD packages needed)
dotnet build LP.Core/LP.Core.csproj

# Build the AutoCAD plugin for one target only
dotnet build LP.Plugin/LP.Plugin.csproj -f net48     # AutoCAD 2024
dotnet build LP.Plugin/LP.Plugin.csproj -f net8.0    # AutoCAD 2025+

# Run all tests
dotnet test LP.Core.Tests/LP.Core.Tests.csproj

# Run tests with verbose output
dotnet test LP.Core.Tests/LP.Core.Tests.csproj -v n

# Run a specific test class
dotnet test LP.Core.Tests/LP.Core.Tests.csproj --filter "FullyQualifiedName~SingleRodProtectionServiceTests"
dotnet test LP.Core.Tests/LP.Core.Tests.csproj --filter "FullyQualifiedName~DoubleRodProtectionServiceTests"

# Run a single test by name
dotnet test LP.Core.Tests/LP.Core.Tests.csproj --filter "FullyQualifiedName=LP.Core.Tests.Services.SN_4_04_03.SingleRodProtectionServiceTests.Calculate_P0900_H50_HxGround_ReturnsExpectedRadius"

# Run tests containing a keyword (e.g., a reliability level)
dotnet test LP.Core.Tests/LP.Core.Tests.csproj --filter "FullyQualifiedName~HxGround"

# Run with code coverage
dotnet test LP.Core.Tests/LP.Core.Tests.csproj --collect:"XPlat Code Coverage"

# Restore NuGet packages
dotnet restore Lightning_Protection.slnx

# Build from repo root with full paths
dotnet build src/Lightning_Protection/Lightning_Protection.slnx
```

## Project Architecture

AutoCAD extension for calculating and visualizing lightning protection areas according to standard SN 4.04.03.

### Solution Projects (`src/Lightning_Protection/`)

- **LP.Core** (`netstandard2.0`) — Core domain logic with zero external dependencies. Contains models, calculation services, and abstraction interfaces for AutoCAD integration. Never references AutoCAD.
- **LP.Core.Tests** (`net8.0`, xUnit, `Xunit` as a global using) — Unit tests for LP.Core calculation services.
- **LP.Plugin** (SDK-style, multi-target `net8.0;net48`) — the single AutoCAD plugin project:
  - `net48` → `AutoCAD.NET` NuGet package **24.3.0** (AutoCAD 2024)
  - `net8.0` → `AutoCAD.NET.Core` NuGet package **25.0.0** (AutoCAD 2025+)
  - both with `ExcludeAssets=runtime` (AutoCAD supplies the DLLs at load time)
  - `LangVersion 12`, `ImplicitUsings` and `Nullable` enabled
- **LP.UI** (`.NET Framework 4.8`, legacy non-SDK csproj) — UI library, currently only `AssemblyInfo`. Targets net48 only, so the `net8.0` build of LP.Plugin cannot reference it as-is.

The `packages/AutoCAD.NET.*.24.0.0/` folder is a gitignored leftover of the former `packages.config` setup; LP.Plugin restores AutoCAD packages from NuGet via `PackageReference`.

### Domain Models

Base models in `LP.Core/Models/Base/` (namespace `LP.Core.Models.Base`):
- `Point3D` (X, Y, Z coordinates)
- `Polyline3D` (list of Point3D with IsClosed flag)
- `ProtectionPoint` (Point3D + optional Radius + IsLastPoint flag)
- `ProtectionArea` (list of ProtectionPoint)
- `OperationResult<T>` — generic result wrapper with IsSuccess/Message/Exception (`Ok(...)` / `Fail(message, exception)`)

Protector models in `LP.Core/Models/LightningProtectors/<Type>/` (namespace `LP.Core.Models.LightningProtectors.<Type>`):
- `SingleRod/SingleRodLightningProtector` — one position + height
- `SingleRod/SingleRodProtectionArea` — center point + radius at height hx
- `SingleWire/SingleWireLightningProtector` — two pin points with heights
- `SingleWire/SingleWireProtectionArea` — two pin points with radii
- `DoubleRod/DoubleRodLightningProtector` — two SingleRodLightningProtectors (composite)
- `DoubleRod/DoubleRodProtectionArea` — both rod areas + zone parameters (h0, r0, L, Lmax, Lc, hc, IsCombined, rx, lx, rcx, lx1, lx2)
- `DoubleWire/DoubleWireLightningProtector` — two SingleWireLightningProtectors (composite)

`ProtectionArea` convention: `Radius == null` → straight segment starts at the point; `Radius` set → arc of that radius starts at the point; `IsLastPoint == true` closes the outline; a single point with a radius is a circle. The double-rod non-combined result is two points with radii = two separate circles (point = centre).

### Calculation Pipeline (SN 4.04.03)

All calculation services follow the same pattern:

1. Service exposes a static `Calculate()` method that returns `OperationResult<T>`; bodies are wrapped in try/catch and exceptions are converted to `Fail` — keep this convention
2. Protected cone model: cone height `h0`, base radius `r0`, protection radius at height hx: `rx = r0 * (h0 - hx) / h0`
3. Three lightning protection reliability levels: 0.900, 0.990, 0.999 (`LightningProtectionReliability.P0_900 / P0_990 / P0_999`)
4. Height-dependent coefficients for three ranges: 0-30m, 30-100m, 100-150m (max 150m). The coefficients come directly from the standard's tables — change them only against the standard
5. Validation: protector height `> 0` and `<= 150` m; `hx >= 0` and `hx < h` (for double rod: less than both heights)

Service implementations (`LP.Core/Services/SN_4_04_03/`):
- `SingleRodProtectionService` — Table 10.1: single rod (fully implemented). `CalculateConeHeight` / `CalculateConeBaseRadius` are reused by the double-rod service.
- `SingleWireProtectionService` — Table 10.2: single wire (fully implemented). Also has `GetProtectionArea()` that computes external tangent lines between two circles.
- `DoubleRodProtectionService` — double rod (implemented). `Calculate()` derives averaged `h0`/`r0`, `Lmax`, `Lc`, `hc` and decides combined vs separate zones; `GetProtectionArea()` returns two circles (L > Lmax) or a 6-point closed outline (separate `hx >= hc` and `hx < hc` builders).
- `LightningProtectionService` — orchestrator implementing `ILightningProtectionService`. Wraps the static services; the double wire method throws `NotImplementedException`.
- `GeometryService` (`LP.Core/Services/`) — static 2D helpers: Get2DDistance, GetAngleToHorizontal, GetTangentDeviationAngle (Z is ignored).

### Key Interfaces (`LP.Core/Interfaces/`)

- `ILightningProtectionService` — main service contract (single rod, single wire, double rod, double wire)
- `IAcadDocument` — document abstraction (Name, ExecuteInTransaction, GetModelSpace, Regen)
- `IAcadTransaction` — transaction abstraction (GetObject, AddEntity, IDisposable)

### AutoCAD Integration

- `LP.Plugin` is currently a stub (project file only). It has **no `ProjectReference` to LP.Core yet** — add `<ProjectReference Include="..\LP.Core\LP.Core.csproj" />` before calling the calculation services.
- Suggested layout inside LP.Plugin: `Acad/` (API helpers, `IAcadDocument` implementation), `Drawing/` (ProtectionArea → AutoCAD entities), `Commands/` (`[CommandMethod]` classes).
- The AutoCAD abstractions in `LP.Core` (`IAcadDocument`, `IAcadTransaction`) are the bridge between calculation logic and the AutoCAD API; LP.Plugin supplies the concrete implementations.
- Code in LP.Plugin must compile for both targets:
  - use `Autodesk.AutoCAD.ApplicationServices.Core.Application` (AcCoreMgd) — the full `ApplicationServices.Application` lives in AcMgd, which the `net8.0` target does not reference;
  - C# 12 syntax is fine, but avoid runtime-dependent features missing in net48 (records/`init`, `required`, `Index`/`Range`, default interface members) and BCL APIs absent in net48 (`Math.Clamp`, `ArgumentNullException.ThrowIfNull`, `string.Contains(char)`, `[NotNullWhen]` and other nullable attributes);
  - use `#if NET8_0_OR_GREATER` for version-specific code.
- AutoCAD API patterns (transactions, layers, polylines with bulges, text, blocks/attributes, annotative scales) and ready-made helpers are in the `autocad-api` skill: `.claude/skills/autocad-api/SKILL.md`.

### Conventions

- `netstandard2.0` LP.Core means **no nullable reference types and no implicit usings** there — write explicit `using` directives and C# 7.3-compatible code in LP.Core. LP.Core.Tests and LP.Plugin use implicit usings and nullable.
- Callers check `OperationResult.IsSuccess` and propagate `Message` / `Exception`; don't throw across project boundaries.

### Implementation Status

- Single rod protection area — **complete** (Table 10.1)
- Single wire protection area — **complete** (Table 10.2, including tangent-line geometry)
- Double rod protection area — **implemented** (`DoubleRodProtectionService`, with tests); geometry of the combined outline still to be verified against the standard
- Double wire protection area — **not implemented** (throws `NotImplementedException`)
- AutoCAD plugin (`LP.Plugin`) — **stub**, no commands yet
