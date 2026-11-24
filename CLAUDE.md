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
- **LP.Plugin** (SDK-style, multi-target `net8.0;net48`) — the single AutoCAD plugin project: `net48` for AutoCAD 2024, `net8.0` for AutoCAD 2025+ (packages and compatibility rules in `LP.Plugin/AGENTS.md`).
- **LP.UI** (`.NET Framework 4.8`, legacy non-SDK csproj) — UI library, currently only `AssemblyInfo`. Targets net48 only, so the `net8.0` build of LP.Plugin cannot reference it as-is.

The `packages/AutoCAD.NET.*.24.0.0/` folder is a gitignored leftover of the former `packages.config` setup; LP.Plugin restores AutoCAD packages from NuGet via `PackageReference`.

### Project-specific guidance

Each project keeps its own knowledge in an `AGENTS.md` next to its csproj. **Read the relevant file before working in that project:**

- [`src/Lightning_Protection/LP.Core/AGENTS.md`](src/Lightning_Protection/LP.Core/AGENTS.md) — C# 7.3 / netstandard2.0 rules, domain models, `ProtectionArea` convention, SN 4.04.03 calculation pipeline, services, `Constants/` and `Helpers/`, interfaces.
- [`src/Lightning_Protection/LP.Plugin/AGENTS.md`](src/Lightning_Protection/LP.Plugin/AGENTS.md) — net48/net8.0 compatibility rules, folder layout, DTO rule, lightning protector commands and `LightningProtectorFinder`, `autocad-api` skill.

### AutoCAD bridge

The AutoCAD abstractions in `LP.Core` (`IAcadDocument`, `IAcadTransaction`) are the bridge between calculation logic and the AutoCAD API; LP.Plugin supplies the concrete implementations. LP.Core never references AutoCAD; LP.Plugin references LP.Core.

### Conventions

- LP.Core is C# 7.3 without nullable or implicit usings; LP.Core.Tests and LP.Plugin use implicit usings and nullable (details in each project's `AGENTS.md`).
- Callers check `OperationResult.IsSuccess` and propagate `Message` / `Exception`; don't throw across project boundaries.
- `/// <summary>` comments on methods:
  - add one only when the method has complex logic. Leave out summaries on simple or self-explanatory methods (getters, thin wrappers, one-line lookups);
  - keep it to an abstract description of what the method does. Leave out implementation details such as property or parameter names, concrete values and constants, or step-by-step behaviour.
- No `/// <summary>` comments on properties or on data types (DTOs and models, such as those in `LP.Plugin/Models/`).

### Implementation Status

- Single rod protection area — **complete** (Table 10.1)
- Single wire protection area — **complete** (Table 10.2, including tangent-line geometry)
- Double rod protection area — **implemented** (`DoubleRodProtectionService`, with tests); geometry of the combined outline still to be verified against the standard
- Double wire protection area — **not implemented** (throws `NotImplementedException`)
- AutoCAD plugin (`LP.Plugin`) — **in progress**: `lightning_protector_insert`, `lightning_protector_edit`, `lightning_protector_toggle_active`, `lightning_protector_delete` and `lightning_protector_focus` implemented; protection-area drawing commands not yet
