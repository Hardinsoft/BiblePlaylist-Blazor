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
