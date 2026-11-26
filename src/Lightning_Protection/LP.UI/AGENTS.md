# LP.UI

Project-specific guidance for `LP.UI`, the WPF user interface. Cross-project rules (build/test commands, solution layout, shared conventions, implementation status) are in the repository root `CLAUDE.md`.

## Targets

SDK-style WPF class library (`UseWPF`), multi-target `net8.0-windows;net48` to match LP.Plugin, `LangVersion 12`, `ImplicitUsings` and `Nullable` enabled; references LP.Core only (never AutoCAD). The same net48 compatibility rules as in `LP.Plugin/AGENTS.md` apply.

## Hosting

`MainWindow` takes an LP.Core `ILightningProtectorRepository` and has two hosts:
- **AutoCAD** — the `lightning_protection_ui` command of LP.Plugin shows it modelessly with `AcadLightningProtectorRepository` (the active drawing).
- **Standalone** — `LP.UI.App` (`net8.0-windows` `WinExe`) shows it with `InMemoryLightningProtectorRepository` (not connected to a drawing). Run from `src/Lightning_Protection/`:

  ```bash
  dotnet run --project LP.UI.App/LP.UI.App.csproj
  ```

Each window or user control merges `Themes/Styles.xaml` itself: there is no WPF `Application` inside AutoCAD, so nothing may depend on `App` resources. The window refreshes on every activation (the drawing may have changed meanwhile).

## Design reference

`design/lightning_protection_ui.html`, first screen (lightning protectors). All user-facing text is in English. The window is `ResizeMode="CanMinimize"` (no maximize).

## Folder layout

- `Infrastructure/` — `ObservableObject` (INotifyPropertyChanged), `RelayCommand`, `RelayCommand<T>` (takes the `CommandParameter`, e.g. a grid row).
- `ViewModels/` — `MainViewModel` (sidebar pages, selected page, source name, refresh), `PageViewModel` (base for sidebar pages: title, subtitle, icon path, refresh), `LightningProtectorsPageViewModel`, `LightningProtectorItemViewModel` (grid row).
- `Views/` — `LightningProtectorsPage` (user control, mapped to its view model by a `DataTemplate` in `MainWindow`).
- `Themes/Styles.xaml` — palette, icons and control styles from the design.

## Lightning protectors page

- Sidebar: only **Lightning protectors** (settings and calculation methods come in follow-up tasks).
- The grid is filled from `ILightningProtectorRepository.GetAll` on every refresh (window activation); the selected row is kept by protector number. A failed load empties the grid and shows its message in the strip under the grid; the next successful load clears it.
- Columns: Status, No., Name, Title (the description is its tooltip), Type, h, Base, X; Y and the go-to-drawing button. Optional values that are not set show as empty cells. For a protector in paper space, X; Y are paper-space coordinates and an info icon says so (with the layout name).
- The go-to-drawing button (`FocusCommand`, takes the row) selects the row and calls `ILightningProtectorRepository.Focus`; the outcome is shown in the strip under the grid.
- The window is 960 px wide so all columns fit without horizontal scrolling; the grid scrolls vertically with a fixed header.
- Toolbar: Add, Edit, Delete and the active/inactive switch, all acting on the selected row. All of them are stubs that report "not available yet" in the strip under the grid — the actions and the Add/Edit dialogs are follow-up tasks.

## Tests

`LP.UI.Tests` (`net8.0-windows`, xUnit) tests the view models against a fake repository:

```bash
dotnet test LP.UI.Tests/LP.UI.Tests.csproj
```
