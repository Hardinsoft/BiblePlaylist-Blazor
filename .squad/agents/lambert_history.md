# Lambert - History (fallback)

## Learnings
- 2026-05-17T14:05:00.638-07:00: Adopted testing patterns for MudBlazor 9.x migration: prefer reflection-based checks for new interfaces to keep tests compiling across versions.
- Async component testing: prefer using reflection to detect presence of Task-returning methods (e.g., ShowMessageBoxAsync) and invoke them via reflection; use Task-based wait assertions in bUnit.
- Component defaults: assert rendered CSS classes and ARIA attributes with bUnit snapshots and class lookups rather than internal fields.
- Converter strategy: detect new IConverter via Type.GetType and run converter tests through reflection to avoid direct compile-time dependency.
- Form components: validate two-way binding via rendered markup and Value/ValueChanged pattern; use reflection for generic components like MudNumericField<T>.
- Dialog and navigation: use IDialogService via reflection if necessary; simulate dialog calls and verify resulting navigation or callback patterns.
- Visual regression: create small, focused bUnit snapshot tests for grid and button variants; do spot-checks, not full pixel diffs.

Note: Unable to create .squad/agents/lambert/history.md due to environment limitations creating new directories; this file is a fallback recorded in .squad/agents/lambert_history.md instead.
