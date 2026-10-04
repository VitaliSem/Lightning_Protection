# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

All commands should be run from the `src/Lightning_Protection/` directory where the solution file lives.

```bash
cd src/Lightning_Protection

# Build the entire solution
dotnet build Lightning_Protection.slnx

# Build only the core library and tests (LP.Plugin.Framework requires
# restore of gitignored local NuGet packages — build LP.Core instead)
dotnet build LP.Core/LP.Core.csproj

# Run all tests
dotnet test LP.Core.Tests/LP.Core.Tests.csproj

# Run tests with verbose output
dotnet test LP.Core.Tests/LP.Core.Tests.csproj -v n

# Run a specific test class
dotnet test LP.Core.Tests/LP.Core.Tests.csproj --filter "FullyQualifiedName~SingleRodProtectionServiceTests"

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

- **LP.Core** (`netstandard2.0`) — Core domain logic with zero external dependencies. Contains models, calculation services, and abstraction interfaces for AutoCAD integration.
- **LP.Core.Tests** (`net8.0`, xUnit) — Unit tests for LP.Core calculation services.
- **LP.Plugin.Framework** (`.NET Framework 4.8`) — AutoCAD plugin referencing AutoCAD 2024 managed DLLs (AcCoreMgd, AcDbMgd, AcMgd, etc.) via local NuGet packages.
- **LP.Plugin.Net8** (`net8.0`) — Modern AutoCAD plugin referencing the AutoCAD.NET NuGet package v25.1.0.
- **LP.UI** (`.NET Framework 4.8`) — Windows Forms UI library (currently only has AssemblyInfo).

### Domain Models

Base models in `LP.Core/Models/Base/`:
- `Point3D` (X, Y, Z coordinates)
- `Polyline3D` (list of Point3D with IsClosed flag)
- `ProtectionPoint` (Point3D + optional Radius + IsLastPoint flag)
- `ProtectionArea` (list of ProtectionPoint)
- `OperationResult<T>` — generic result wrapper with IsSuccess/Message/Exception

Protector models in `LP.Core/Models/LightningProtectors/`:
- `SingleRodLightningProtector` — one position + height
- `SingleRodProtectionArea` — center point + radius at height hx
- `SingleWireLightningProtector` — two pin points with heights
- `SingleWireProtectionArea` — two pin points with radii
- `DoubleRodLightningProtector` — two SingleRodLightningProtectors (composite)
- `DoubleWireLightningProtector` — two SingleWireLightningProtectors (composite)

### Calculation Pipeline (SN 4.04.03)

All calculation services follow the same pattern:

1. Service exposes a static `Calculate()` method that returns `OperationResult<T>`
2. Protected cone model: cone height `h0`, base radius `r0`, protection radius at height hx: `rx = r0 * (h0 - hx) / h0`
3. Three lightning protection reliability levels: 0.900, 0.990, 0.999
4. Height-dependent coefficients for three ranges: 0-30m, 30-100m, 100-150m (max 150m)

Service implementations:
- `LP.Core/Services/SN_4_04_03/` contains the actual calculation logic
- `SingleRodProtectionService` — Table 10.1: single rod (fully implemented)
- `SingleWireProtectionService` — Table 10.2: single wire (fully implemented). Also has `GetProtectionArea()` that computes external tangent lines between two circles.
- `LightningProtectionService` — orchestrator implementing `ILightningProtectionService`. Wraps the static services; double rod and double wire methods throw `NotImplementedException`.
- `GeometryService` — static helpers: Get2DDistance, GetAngleToHorizontal, GetTangentDeviationAngle

### Key Interfaces (`LP.Core/Interfaces/`)

- `ILightningProtectionService` — main service contract (single rod, single wire, double rod, double wire)
- `IAcadDocument` — document abstraction (Name, ExecuteInTransaction, GetModelSpace, Regen)
- `IAcadTransaction` — transaction abstraction (GetObject, AddEntity, IDisposable)

### AutoCAD Integration

- `LP.Plugin.Framework` references AutoCAD 2024 DLLs from local NuGet packages (`packages/AutoCAD.NET.*.24.0.0/`)
- `LP.Plugin.Net8` references `AutoCAD.NET` NuGet package v25.1.0 (`net8.0`)
- Both AutoCAD plugin projects are currently stubs (only project files + assembly info)
- The AutoCAD abstractions in `LP.Core` (`IAcadDocument`, `IAcadTransaction`) are the bridge between calculation logic and the AutoCAD API

### Implementation Status

- Single rod protection area — **complete** (Table 10.1)
- Single wire protection area — **complete** (Table 10.2, including tangent-line geometry)
- Double rod protection area — **not implemented** (throws `NotImplementedException`)
- Double wire protection area — **not implemented** (throws `NotImplementedException`)