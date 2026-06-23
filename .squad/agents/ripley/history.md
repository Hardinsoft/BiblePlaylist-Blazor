# History — Ripley (Lead Architect)

## Project Context
- **Repo:** BiblePlaylist-Blazor (Hardinsoft)
- **Framework:** Blazor WASM frontend + ASP.NET Server backend
- **Target:** .NET 10
- **User:** Greg Hardin

## Key Milestones
- ✅ .NET 9 → 10 upgrade (PR #2, merged)
- ✅ Package audit and selective updates (no vulnerabilities)
- 🔄 MudBlazor 7.15.0 → 8.15.0 migration (PR #4, under review)
- ✅ bUnit test infrastructure added

## MudBlazor Migration Strategy
- Isolated to branch `releases/mudblazor-upgrade-8x`
- Version bumped to 8.15.0 (safe, stable)
- Attribute casing fixed to satisfy MudBlazor v8 analyzers
- Deprecated IActionContextAccessor removed
- Unused fields cleaned (VersionInit._httpAudio, ReadParseVerses._selectedBookChapter, NavBibleMenu.razor restored)
- Tests added: smoke tests for component validation
- PR #4 ready for review

## Decisions Made
- Keep MudBlazor migration separate from .NET upgrade for cleaner PR history
- No vulnerable packages to address
- Add bUnit tests to prevent regressions during migrations

## Current State
- Build: ✅ Succeeds with minimal warnings (8 non-critical)
- Tests: ✅ Pass (bUnit smoke tests)
- PR #4: Open, awaiting Dallas & Parker review

## Learnings
- Project structure: Blazor WASM client in BiblePlaylist\Client, server in BiblePlaylist\Server, tests in Test. MudBlazor assets referenced in Client (csproj and index.html).
- Converter patterns: No custom *Converter classes were found in the codebase; JSON/serialization converters are not present. Migration unlikely to require converter rewrites.
- Form components: No classes inherit from MudFormComponent. Forms use Mud components in Razor pages (MudNumericField, MudSwitch, MudButton, MudFab, MudGrid, MudItem, MudList, MudNavLink).
- Dialogs and services: MainLayout includes <MudDialogProvider /> and <MudSnackbarProvider />; there are no programmatic DialogService.Show/ShowMessageBox usages in application code (tests register dialog services). Update tests and provider wiring as needed.
- Theming: MudThemeProvider is used in MainLayout; no MudGlobal occurrences found. Verify theme API/property names in v9.
- Async usage: Many async Task methods exist across client Razor components and server controllers; review any MudBlazor async API changes (dialogs/snackbars) that may affect awaiting patterns.
- Test strategy: Expand bUnit coverage for affected components and add visual regression snapshots. CI should run these before merging.
- Risk areas: Component API renames and styling/visual regressions are highest risk; dialogs/tests are medium risk; converters and globals are low risk.

