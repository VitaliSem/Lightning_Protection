# LP.UI

Project-specific guidance for `LP.UI`, the WPF user interface. Cross-project rules (build/test commands, solution layout, shared conventions, implementation status) are in the repository root `CLAUDE.md`.

## Targets

SDK-style WPF class library (`UseWPF`), multi-target `net8.0-windows;net48` to match LP.Plugin, `LangVersion 12`, `ImplicitUsings` and `Nullable` enabled; references LP.Core only (never AutoCAD). The same net48 compatibility rules as in `LP.Plugin/AGENTS.md` apply.

## Hosting

`MainWindow` takes an LP.Core `ILightningProtectorRepository` and has two hosts:
- **AutoCAD** — the `lightning_protection_ui` command of LP.Plugin shows it modelessly with `AcadLightningProtectorRepository` (the active drawing).
- **Standalone** — `LP.UI.App` (`net8.0-windows` `WinExe`) shows it with `InMemoryLightningProtectorRepository` (not connected to a drawing; added protectors stay in memory, picking a position is not available). Run from `src/Lightning_Protection/`:

  ```bash
  dotnet run --project LP.UI.App/LP.UI.App.csproj
  ```

Each window or user control merges `Themes/Styles.xaml` itself: there is no WPF `Application` inside AutoCAD, so nothing may depend on `App` resources. The window refreshes on every activation (the drawing may have changed meanwhile).

## Design reference

`design/lightning_protection_ui.html`, first screen (lightning protectors). All user-facing text is in English. The window is `ResizeMode="CanMinimize"` (no maximize).

## Folder layout

- `Infrastructure/` — `ObservableObject` (INotifyPropertyChanged), `RelayCommand`, `RelayCommand<T>` (takes the `CommandParameter`, e.g. a grid row), `ValidatableViewModel` (`ObservableObject` with per-property errors through `INotifyDataErrorInfo`: `SetError`, `GetError`, `HasErrors`).
- `ViewModels/` — `MainViewModel` (sidebar pages, selected page, source name, refresh), `PageViewModel` (base for sidebar pages: title, subtitle, icon path, refresh), `LightningProtectorsPageViewModel`, `LightningProtectorItemViewModel` (grid row), `AddLightningProtectorViewModel` (add dialog).
- `Views/` — `LightningProtectorsPage` (user control, mapped to its view model by a `DataTemplate` in `MainWindow`), `AddLightningProtectorDialog` (modal window).
- `Services/` — `IDialogService` (opens modal dialogs; replaced by a fake in the tests) and its implementation `DialogService` (created by `MainWindow`, which owns the dialogs).
- `Themes/` — resources from the design, split by type. `Styles.xaml` is the single entry point that views merge; it only merges the files below:
  - `Colors.xaml` — palette brushes.
  - `Icons.xaml` — icon geometries (16×16 box), `AppIcon`, `IconPath` style.
  - `Typography.xaml` — `HintText`, `PageHeader`, `FieldLabel`.
  - `Controls/Buttons.xaml` — `Btn`, `BtnPrimary`, `BtnGhostIcon`, `BtnToggleActive`.
  - `Controls/Inputs.xaml` — `Input`, `UnitInput` (suffix in `Tag`), `Select`, `Check`; validation: `FieldErrorTemplate` (default `Validation.ErrorTemplate` of `Input`: red border and the message under the field), `GroupErrorTemplate` (red border only) and `ValidatedFieldMargin`.
  - `Controls/Containers.xaml` — `Card`, `InfoBox`, `ErrorBox`.
  - `Controls/Navigation.xaml` — `NavItem` (sidebar).
  - `Controls/DataGrid.xaml` — `GridHeader`, `GridCell`, `GridRow`.

  A `StaticResource` in a merged dictionary does not see its sibling dictionaries, so every file merges the files it references itself (e.g. `Colors.xaml`). Use full `/LP.UI;component/Themes/...` URIs. New resources go to the file of their type, and a new file is added to `Styles.xaml`.

## Lightning protectors page

- Sidebar: only **Lightning protectors** (settings and calculation methods come in follow-up tasks).
- The grid is filled from `ILightningProtectorRepository.GetAll` on every refresh (window activation); the selected row is kept by protector number. A failed load empties the grid and shows its message in the strip under the grid; the next successful load clears it.
- Columns: Status, No., Name, Title (the description is its tooltip), Type, h, Base, X; Y and the go-to-drawing button. Optional values that are not set show as empty cells. For a protector in paper space, X; Y are paper-space coordinates and an info icon says so (with the layout name).
- The go-to-drawing button (`FocusCommand`, takes the row) selects the row and calls `ILightningProtectorRepository.Focus`; the outcome is shown in the strip under the grid.
- The window is 960 px wide so all columns fit without horizontal scrolling; the grid scrolls vertically with a fixed header.
- Toolbar: Add, then Edit, Delete and the active/inactive switch acting on the selected row. Edit, Delete and the switch are stubs that report "not available yet" in the strip under the grid — the actions and the Edit dialog are follow-up tasks.

## Add dialog

- **Add** opens `Views/AddLightningProtectorDialog` (modal, owned by the main window) through `Services/IDialogService` (`Services/DialogService`; the tests use a fake). Layout follows the design's "Add lightning protector" modal: Name / Title, Type, Height h (required) / Base height, Position X / Y with **Pick**, Description, the disabled **Active** checkbox (always checked: a new protector is always active) and the disabled **Save to database as a template** checkbox (follow-up task).
- Type is optional: the selector starts at **None** (`AddLightningProtectorViewModel.NoType`, a UI-only entry meaning an empty type), followed by the fixed list of five types (LP.Core `LightningProtectorTypeOption.All`, shared with the `lightning_protector_insert` command). Each entry shows its description as a tooltip. The type name, or an empty value for None, is stored in the `TYPE` attribute.
- `AddLightningProtectorViewModel` (a `ValidatableViewModel`) validates on **Add** (required height in (0; 150] m, numbers accept `.` or `,`, X and Y required, text lengths from `LightningProtectorBlockService`, also capped by the boxes' `MaxLength`); errors then follow every change.
  - An error of one field is set on its property and shown under that field by `FieldErrorTemplate`. The template draws in the adorner layer and does not take space, so every validated field keeps `ValidatedFieldMargin` free below it.
  - An error shared by several fields (X and Y) is set on each of them, whose boxes use `GroupErrorTemplate` (outline only), and its message goes once to the box at the bottom of the form. That box (`ErrorMessage`) also shows the outcome of a failed insert or pick.
- Valid input goes to `ILightningProtectorRepository.Insert`, which numbers the protector (next free number, shown as a hint) and adds it as active. A failed insert keeps the dialog open with its message; a successful one closes it, and the page reloads, selects the new row and reports it in the strip. Cancel or the close button never inserts.
- **Pick** closes the dialog (`IsPickRequested`), the dialog service hides the main window, calls `ILightningProtectorRepository.PickPosition` and reopens the dialog with the same view model; a failed pick keeps the entered coordinates and shows the message.

## Tests

`LP.UI.Tests` (`net8.0-windows`, xUnit) tests the view models (`ViewModels/`) against the fakes in `Fakes/` (namespace `LP.UI.Tests.Fakes`): `FakeLightningProtectorRepository` and `FakeDialogService`:

```bash
dotnet test LP.UI.Tests/LP.UI.Tests.csproj
```
