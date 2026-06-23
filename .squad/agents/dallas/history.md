# Dallas Agent History

## Learnings

2026-05-17T14:05:00.638-07:00 - MudBlazor 9.x audit summary:

- Components scanned in BiblePlaylist/Client/: MudGrid, MudItem, MudButton, MudNumericField, MudFab, MudList, MudToggleIconButton, MudExpansionPanel, MudAlert, MudText, MudPaper, MudContainer, MudAppBar, MudDrawer, MudDialogProvider.
- Files with notable usages:
  - Client\Shared\NavBibleMenu.razor — MudGrid, MudItem, MudFab, MudList, MudToggleIconButton
  - Client\Shared\NavPlaylistMenu.razor — MudGrid, MudItem, MudFab, MudList, MudExpansionPanels, MudButton (one explicit Variant)
  - Client\Shared\ReadParseVerses.razor — MudGrid, MudItem, MudFab, MudNumericField, MudButton (Save)
  - Client\Shared\PlaylistPlayer.razor — MudGrid, MudItem
  - Client\Shared\ReadVerses.razor — no MudBlazor components beyond layout

- API compatibility findings:
  - No usages found of removed/renamed component methods (e.g., ActivatePanel, Clear on MudSelect/MudList, ShowMessageBox) in the Client project.
  - No custom Converter<T> implementations or overrides of MudFormComponent protected methods (WriteValueAsync, SetTextAsync, etc.).
  - MudNumericField usages bind to decimal values; default converters in v9 should handle these without change, but custom converters would need rewriting.
  - Some MudButton usages relied on implicit/global defaults; v9 removes MudGlobal button/input defaults. Where behavior matters (action buttons), prefer explicit Variant/Color.

- Quick fixes applied:
  - 2026-05-17T14:05:00.638-07:00: Set Save button in Client\Shared\ReadParseVerses.razor to Variant.Filled and Color.Primary (explicit to match previous visual intent).

- Risky areas for testing (suggested focus for Lambert):
  1. Components that previously relied on MudGlobal defaults: ensure buttons/inputs still look and behave as intended (Nav menus, action buttons, form inputs).
  2. Any custom converters or wrapper input components (none found in Client/, but Shared/ or Server/ may contain them). If present, they must be rewritten to IConverter<TIn,TOut>/IReversibleConverter.
  3. Dialog usage and DialogService APIs across the app — ensure Show* calls use Async variants (no instances found in Client/, but Server/Shared code audit recommended).
  4. MudMenu / Activator patterns — none found, but if introduced later, activator signatures changed.
  5. MudFormComponent-derived custom inputs (none found) — overrides must be renamed to SetValueCoreAsync/SetTextCoreAsync.

- Recommendations:
  - Add a small visual smoke test for key pages (Nav menus, playlist page, verse editor) to catch style/default regressions.
  - If app-wide defaults are desired, implement lightweight wrapper components (e.g., AppButton) to centralize Variant/Color instead of relying on MudGlobal.
  - Run a dependency upgrade of MudBlazor in a feature branch and have CI run full builds/tests; I could not run dotnet build in this environment (pwsh missing) — CI must validate compilation.

