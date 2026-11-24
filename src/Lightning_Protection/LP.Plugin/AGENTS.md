# LP.Plugin

Project-specific guidance for `LP.Plugin`, the AutoCAD plugin. Cross-project rules (build/test commands, solution layout, shared conventions, implementation status) are in the repository root `CLAUDE.md`.

## Targets and compatibility

SDK-style, multi-target `net8.0;net48`:
- `net48` → `AutoCAD.NET` NuGet package **24.3.0** (AutoCAD 2024)
- `net8.0` → `AutoCAD.NET.Core` NuGet package **25.0.0** (AutoCAD 2025+)
- both with `ExcludeAssets=runtime` (AutoCAD supplies the DLLs at load time)
- `LangVersion 12`, `ImplicitUsings` and `Nullable` enabled

Code must compile for both targets:
- use `Autodesk.AutoCAD.ApplicationServices.Core.Application` (AcCoreMgd) — the full `ApplicationServices.Application` lives in AcMgd, which the `net8.0` target does not reference;
- C# 12 syntax is fine, but avoid runtime-dependent features missing in net48 (records/`init`, `required`, `Index`/`Range`, default interface members) and BCL APIs absent in net48 (`Math.Clamp`, `ArgumentNullException.ThrowIfNull`, `string.Contains(char)`, `[NotNullWhen]` and other nullable attributes);
- use `#if NET8_0_OR_GREATER` for version-specific code.

`LP.UI` targets net48 only, so the `net8.0` build cannot reference it as-is.

## Folder layout

- `Acad/` — generic AutoCAD API helpers adapted from the `autocad-api` skill: `BlockHelper` (definitions, inserts, references), `BlockAttributeHelper` (attribute definitions and references, reading and writing values), `LayerHelper`, `ScaleHelper`, `PromptHelper` (reusable `TryGet...` editor prompts for commands).
- `Drawing/` — protector-specific drawing code: block definitions, ProtectionArea → AutoCAD entities, locating protector references.
- `Commands/` — `[CommandMethod]` classes, each with a `<Name>Prompts` class holding its prompt texts and prompt DTOs.
- `Models/` — DTOs: `LayerDto`, `BlockDefinitionDto`, `AttributeDefinitionDto`, `BlockInsertDto`, `TextPromptDto`, `NumberPromptDto`, `LightningProtectorReferenceDto`.
- `Constants/` — names that must not be hard-coded elsewhere: `AcadConstants` (AutoCAD key names such as object context collections) and `CommandNames` (names of `[CommandMethod]` commands).

Methods that would take more than 3 parameters take a DTO instead.

## Lightning protector block and commands

- `lightning_protector_insert` (`Commands/LightningProtectorCommands`) inserts the annotative block `B_LP_Lightning_Protector` (layer `L_LP_Lightning_Protectors`, created on demand by `Drawing/LightningProtectorBlock`). Data rules (names, numbering, validation, attribute conversion) come from LP.Core: `LightningProtectorBlockService`, `Constants/LightningProtectorAttributes`, `Helpers/AttributeValueHelper`.
- `lightning_protector_edit` (same class) finds a protector block by its `NUMBER` attribute and edits every other attribute (name, title, type, height, base height, description, is active), using the current values as prompt defaults: Enter keeps a value, and `.` (`PromptHelper.ClearTextInput`) clears an optional one (texts and base height). Only attribute texts change; the reference's placement is untouched.
- `lightning_protector_toggle_active` (same class) finds a protector block by its `NUMBER` attribute and flips only its `IS_ACTIVE` attribute. The new value comes from `LightningProtectorBlockService.ToggleActive`, which parses the status strictly (`true`/`false`, blank counts as inactive) and fails when the attribute is missing or cannot be interpreted; find, toggle and write happen in one transaction, so any failure leaves the drawing unchanged.
- `Drawing/LightningProtectorFinder` locates protector references: `FindAll`, `GetExistingNumbers`, and `FindByNumber` (returns `OperationResult<LightningProtectorReferenceDto>`, failing with a ready message when the number is missing or duplicated). Reuse it in any command that works on a protector by its number (edit, delete, …).
- Command pattern: get the active document via `AcApp.DocumentManager.MdiActiveDocument`, collect input with `PromptHelper` before opening a transaction, check every `OperationResult` and print its `Message`, and catch `Autodesk.AutoCAD.Runtime.Exception` and `System.Exception` separately.

## AutoCAD API reference

AutoCAD API patterns (transactions, layers, polylines with bulges, text, blocks/attributes, annotative scales) and ready-made helpers are in the `autocad-api` skill: `.claude/skills/autocad-api/SKILL.md`.
