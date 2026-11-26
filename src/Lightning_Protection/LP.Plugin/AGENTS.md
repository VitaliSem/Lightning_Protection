# LP.Plugin

Project-specific guidance for `LP.Plugin`, the AutoCAD plugin. Cross-project rules (build/test commands, solution layout, shared conventions, implementation status) are in the repository root `CLAUDE.md`.

## Targets and compatibility

SDK-style, multi-target `net8.0-windows;net48`, `UseWPF` (to host the LP.UI window):
- `net48` → `AutoCAD.NET` NuGet package **24.3.0** (AutoCAD 2024)
- `net8.0-windows` → `AutoCAD.NET` **25.0.0** plus an explicit `AutoCAD.NET.Core` **25.0.0** (AutoCAD 2025+). `AutoCAD.NET` 25.0.0 declares a dependency on the unpublished `AutoCAD.NET.Core 25.0.0-V058`, so Core is pinned directly and `NU1608` is suppressed.
- all with `ExcludeAssets=runtime` (AutoCAD supplies the DLLs at load time)
- references LP.Core and LP.UI
- `LangVersion 12`, `ImplicitUsings` and `Nullable` enabled

Code must compile for both targets:
- prefer `Autodesk.AutoCAD.ApplicationServices.Core.Application` (AcCoreMgd, aliased `AcApp`); use the full `ApplicationServices.Application` (AcMgd) only for what Core lacks, such as `ShowModelessWindow`;
- C# 12 syntax is fine, but avoid runtime-dependent features missing in net48 (records/`init`, `required`, `Index`/`Range`, default interface members) and BCL APIs absent in net48 (`Math.Clamp`, `ArgumentNullException.ThrowIfNull`, `string.Contains(char)`, `[NotNullWhen]` and other nullable attributes);
- use `#if NET8_0_OR_GREATER` for version-specific code.

## Folder layout

- `Acad/` — generic AutoCAD API helpers adapted from the `autocad-api` skill: `BlockHelper` (definitions, inserts, references), `BlockAttributeHelper` (attribute definitions and references, reading and writing values), `LayerHelper`, `ScaleHelper`, `PromptHelper` (reusable `TryGet...` editor prompts for commands), `ViewHelper` (layout switching, entity extents, zooming the current view to a WCS window).
- `Drawing/` — protector-specific drawing code: block definitions, ProtectionArea → AutoCAD entities, locating protector references, `AcadLightningProtectorRepository` (the drawing as LP.Core `ILightningProtectorRepository` for the UI).
- `Commands/` — `[CommandMethod]` classes, each with a `<Name>Prompts` class holding its prompt texts and prompt DTOs.
- `Models/` — DTOs: `LayerDto`, `BlockDefinitionDto`, `AttributeDefinitionDto`, `BlockInsertDto`, `TextPromptDto`, `NumberPromptDto`, `LightningProtectorReferenceDto`.
- `Constants/` — names that must not be hard-coded elsewhere: `AcadConstants` (AutoCAD key names such as object context collections) and `CommandNames` (names of `[CommandMethod]` commands, grouped in nested classes by command class: `LightningProtector`, `UserInterface`).

Methods that would take more than 3 parameters take a DTO instead.

## Lightning protector block and commands

- `lightning_protector_insert` (`Commands/LightningProtectorCommands`) inserts the annotative block `B_LP_Lightning_Protector` (layer `L_LP_Lightning_Protectors`, created on demand by `Drawing/LightningProtectorBlock`). Data rules (names, numbering, validation, attribute conversion) come from LP.Core: `LightningProtectorBlockService`, `Constants/LightningProtectorAttributes`, `Helpers/AttributeValueHelper`.
- `lightning_protector_edit` (same class) finds a protector block by its `NUMBER` attribute and edits every other attribute (name, title, type, height, base height, description, is active), using the current values as prompt defaults: Enter keeps a value, and `.` (`PromptHelper.ClearTextInput`) clears an optional one (texts and base height). Only attribute texts change; the reference's placement is untouched.
- `lightning_protector_toggle_active` (same class) finds a protector block by its `NUMBER` attribute and flips only its `IS_ACTIVE` attribute. The new value comes from `LightningProtectorBlockService.ToggleActive`, which parses the status strictly (`true`/`false`, blank counts as inactive) and fails when the attribute is missing or cannot be interpreted; find, toggle and write happen in one transaction, so any failure leaves the drawing unchanged.
- `lightning_protector_delete` (same class) finds a protector block by its `NUMBER` attribute, asks for a Yes/No confirmation (default No; the name is read from the raw attribute so a protector with invalid attributes can still be deleted) and erases only that block reference. The block definition is never erased or purged, even after the last reference is deleted.
- `lightning_protector_focus` (same class) asks for a number and runs `Drawing/LightningProtectorFocus.Focus`, which finds the protector block by its `NUMBER` attribute, makes the Model tab or the layout that owns it current (paper space on a layout), zooms so the block's extents fill a fixed share of the view (window size from LP.Core `ViewFocusService`), and leaves the block selected (`CommandFlags.Redraw` keeps the implied selection). It warns when the block's layer is off or frozen; the drawing itself is never modified. `LightningProtectorFocus` returns its messages as an `OperationResult<string>` and is shared with the UI window's go-to-drawing button.
- `Drawing/LightningProtectorFinder` locates protector references: `FindAll` (attribute values, insertion point and, for paper space, the layout name), `GetExistingNumbers`, and `FindByNumber` (returns `OperationResult<LightningProtectorReferenceDto>`, failing with a ready message when the number is missing or duplicated). Reuse it in any command that works on a protector by its number (edit, delete, …).
- Command pattern: get the active document via `AcApp.DocumentManager.MdiActiveDocument`, collect input with `PromptHelper` before opening a transaction, check every `OperationResult` and print its `Message`, and catch `Autodesk.AutoCAD.Runtime.Exception` and `System.Exception` separately.

## UI window

- `lightning_protection_ui` (`Commands/UserInterfaceCommands`) opens the LP.UI `MainWindow` as a modeless window (`Application.ShowModelessWindow`, AcMgd). One window per session: running the command again restores and activates it.
- The window gets a `Drawing/AcadLightningProtectorRepository`, which always works on the *active* document. The window is modeless, so every drawing access takes `doc.LockDocument()`.
  - `GetAll` reads all protector references (model space and layouts) with `LightningProtectorFinder.FindAll` and `LightningProtectorBlockService.FromAttributeValues`, ordered by number. A required attribute (number, height) is assumed valid; a reference that still cannot be read is left out so it cannot break the table. Optional attributes that are not set come back as empty values.
  - `Focus` runs `LightningProtectorFocus.Focus` and then hands the keyboard to the drawing view (`Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView`).
  - Changing protectors from the window comes in a follow-up task.
- The window refreshes whenever it is activated, so switching drawings shows up when the user returns to it.

## AutoCAD API reference

AutoCAD API patterns (transactions, layers, polylines with bulges, text, blocks/attributes, annotative scales) and ready-made helpers are in the `autocad-api` skill: `.claude/skills/autocad-api/SKILL.md`.
