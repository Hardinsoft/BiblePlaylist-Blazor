# Segment CRUD (Edit + Delete) — Plan

**Project:** BiblePlaylist-Blazor  
**Date:** 2026-08-29  
**Status:** Approved, ready to implement  

---

## Context Snapshot (from reading codebase)

### Current state

- **NavPlaylistMenu.razor** (709 lines) — playlist selector, chapter expansion panels with segments, drag-drop reorder of chapters, segment playback with verse highlighting + scroll tracking via `_playbackTimer` (300ms) → `UpdatePlaybackPosition()` → `_dropContainer.Refresh()` + `js.InvokeVoidAsync("scrollToVerse", ...)`.
- **Segment.cs** — `IList<Verse> Verses`, `int VerseStart`, `int VerseEnd`. Computed `VoiceText`.
- **BookChapter.cs** — `Book`, `Chapter`, `IList<Segment> Segments`, computed `Display` (e.g. "John 3:1-9; 14-16").
- **PlaylistController.cs** — `POST /playlist/inflate` takes `BookChapters` with segment verse ranges, pulls full `Chapter.Verses` from version repo, filters verses into each segment's `Verses` list.
- **Read.razor** + **ReadVerses.razor** — bare-bones chapter reader: loads a `Version`, fills `displayVerses`, responds to `delegateLibrary.ChapterChanged`. No segment awareness, no selection UI.
- **NavBibleMenu.razor** — standalone bible navigation with its own `AudioPlayer`, full-chapter playback, `ChapterChanged` events.
- **AudioPlayer.razor** — reusable audio component; keeps `<audio>` always in DOM; uses `DotNetObjectReference` + `[JSInvokable]` + `OnAfterRenderAsync`; supports segment vs full-chapter playback.
- **Library save:** `PUT /library` with the full `Library` DTO.
- **JS interop layer:** `wwwroot/js/audioplayer.js` (271 lines) — `initializeAudioPlayer`, `loadAudioFile`, `PlayAudioSegment`, `GetAudioCurrentTime`, `pauseAudioPlayer`, `playAudioPlayer`, `seekAudioPlayer`, `scrollToVerse`, `speakSequenceAsync`, `SetNetObject`, `NavToBook`, `ScrollToTop`. Clean and agent-readable.
- **Tests:** `BiblePlaylist.Tests` uses Bunit 1.40.0 + Moq + xunit, references Client and Shared projects.
- **CSS:** Bootstrap + MudBlazor 9.8.0.

### Constraints accepted

- Segments must stay within a single chapter (audio is recorded per chapter).
- Segments are contiguous verse ranges only — non-contiguous selections require separate segments.
- Create + edit in the same page; create is minimal on this first pass (empty selection → save creates first segment).
- Query-string routing for the editor page.
- Delete = Snackbar undo-style confirmation (no dialog prompt).
- Playback during editing: in-progress segment (uses the new verse range, not the saved one).
- "Add segment to playlist from Read.razor" is future work, not this sprint.

---

## Files in Scope

| File | Action | Risk |
|---|---|---|
| `NavPlaylistMenu.razor` | Add triple-dots `MudMenu` per segment card; "Edit segment" + "Delete segment" items; `NavigationManager` injection | Low — display/navigation only |
| `Pages/SegmentEditor.razor` | **New.** Query-string routed edit/create page. Verse display with selection, Save/Cancel, in-progress playback. | Medium — new page |
| `SegmentPlaybackHelper.cs` (Shared or Client) | **New.** Extract playback timer, position poll, verse-ID resolution, play/pause/seek/cancel from NavPlaylistMenu so both pages reuse it without copying. | Medium — extraction |
| `wwwroot/js/audioplayer.js` | No changes — editor reuses existing functions. | None |
| `PlaylistController.cs` | No new endpoint required. Optional out-of-range validation later. | None |
| `BiblePlaylist.Tests/` | Add Bunit tests for editor page (render, select verse, save, cancel, playback highlight, delete). | Low |

### Out of scope (untouched)

`Read.razor`, `ReadVerses.razor`, `NavBibleMenu.razor`, `AudioPlayer.razor`, `AppButton.razor`, `AppFab.razor`, `MainLayout.razor`, `Program.cs`, `index.html`.

---

## 1. Triple-dots Menu in NavPlaylistMenu

### Current segment card (lines ~105-133)

```razor
<MudPaper Class="..." Style="..." @onclick="...PlaySpecificSegment...">
    <div class="verse-container">
        @foreach (var verse in segment.Verses) { ... }
    </div>
</MudPaper>
```

### Changed card

```razor
<MudPaper Class="..." Style="..." @onclick="...PlaySpecificSegment...">
    <div class="d-flex justify-content-between verse-container">
        <div class="verse-list">
            @foreach (var verse in segment.Verses) { ... }
        </div>
        <MudMenu Icon="@Icons.Material.Filled.MoreVert"
                 AriaLabel="Segment options"
                 @onclick:stopPropagation="true">
            <MudMenuItem Label="Edit segment"
                         @onclick="() => NavigateToEdit(chapter, segment)" />
            <MudMenuItem Label="Delete segment"
                         Color="Color.Error"
                         @onclick="() => DeleteSegment(chapter, segment)" />
        </MudMenu>
    </div>
</MudPaper>
```

### Notes

- `@onclick:stopPropagation="true"` on `MudMenu` so clicking the dots doesn't trigger the card's play handler.
- `Icon` = `Icons.Material.Filled.MoreVert` — the de-facto triple-dots in MudBlazor (verified in source: `MenuIconButtonsExample.razor`).
- `NavigateToEdit` builds query string from segment's `VerseStart`/`VerseEnd` + chapter book/chapter numbers + playlist key.
- `DeleteSegment` removes segment from `chapter.Segments`, rebuilds playback list, saves library, shows Snackbar confirmation, refreshes drop container. If chapter ends up with zero segments, show `MudAlert` warning inline.

### New injection

`NavigationManager` — already available in Blazor (no extra package).

### Delete behavior

- No confirmation dialog. Delete immediately, show Snackbar with "Segment deleted" + optional undo (re-add the segment with a timer). Since segments are easy to recreate and the user explicitly asked for snackbar undo-style, I'll show a Snackbar on delete. Undo re-adds the segment if clicked within a short window (e.g. 5 seconds). If undo is too much for the first pass, just show the confirmation Snackbar.

---

## 2. SegmentEditor.razor — New Page

### Route

```razor
@page "/edit-segment"
```

### Query parameters

| Param | Purpose | When missing |
|---|---|---|
| `book` | Book number | Invalid — show error / navigate back |
| `chapter` | Chapter number | Invalid — show error / navigate back |
| `playlist` | Playlist key | Invalid in edit mode; in create mode, required to know which playlist to add to |
| `segmentStart` | Existing segment's VerseStart | Missing → create mode (empty selection) |
| `segmentEnd` | Existing segment's VerseEnd | Missing → create mode |

### Mode detection

- **Edit** = all five params present and a matching segment found in the playlist.
- **Create** = `segmentStart`/`segmentEnd` missing (or no matching segment) → page renders with empty selection; Save creates the first segment in the chapter.

### Parameter binding approach

**Primary:** `[Parameter] [SupplyParameterFromQuery]` — standard Blazor .NET 6+ feature, available in .NET 10. Cleanest option.

```razor
@code {
    [Parameter] [SupplyParameterFromQuery] public int Book { get; set; }
    [Parameter] [SupplyParameterFromQuery] public int Chapter { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string Playlist { get; set; }
    [Parameter] [SupplyParameterFromQuery] public int SegmentStart { get; set; }
    [Parameter] [SupplyParameterFromQuery] public int SegmentEnd { get; set; }
}
```

**Fallback:** Manual parse from `NavigationManager.Uri` + `HttpUtility.ParseQueryString` if binding doesn't work for some reason.

### Lifecycle

**OnInitializedAsync / OnParametersSetAsync:**

1. Read query params (via binding or manual parse).
2. Fetch library (or use cached) → locate `Playlist` by key → locate `BookChapter` by book+chapter → locate `Segment` by verse range (edit mode).
3. Fetch full chapter verses via `Version?key=...&Book=..&Chapter=..` (same endpoint ReadVerses uses) → populate display verse list.
4. Compute initial selection:
   - Edit mode: verses in `[VerseStart, VerseEnd]` are selected.
   - Create mode: selection is empty.

### Render

```razor
<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-3">
    <!-- Top bar: chapter ref + Play/Stop + Save/Cancel -->
    <MudStack Row Spacing="2" Class="mb-3" Align="Align.Center">
        <MudText Typo="Typo.h5" Class="flex-grow-1">@chapterReference</MudText>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="TogglePlayback">
            @if (_isPlaying) {
                <MudIcon Icon="@Icons.Material.Filled.Stop" />
            } else {
                <MudIcon Icon="@Icons.Material.Filled.PlayArrow" />
            }
            @(_isPlaying ? "Stop" : "Play segment")
        </MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SaveSegment">Save</MudButton>
        <MudButton Variant="Variant.Text" Color="Color.Secondary" OnClick="CancelEdit">Cancel</MudButton>
    </MudStack>

    <MudAlert Severity="Severity.Info" Variant="Variant.Outlined" Class="mb-3">
        Segment: @(selection.Count == 0 ? "not yet defined" : $"{selection.Min()}-{selection.Max()} ({selection.Count} verses)")
    </MudAlert>

    <!-- Verse list -->
    @foreach (var verse in displayVerses) {
        <MudPaper Class="pa-2 mb-2"
                  Style="cursor: pointer; @(selection.Contains(verse.Number) ? "background-color: rgba(var(--mud-palette-primary-rgb), 0.10); border-left: 4px solid var(--mud-palette-primary);" : "")">
            <div class="d-flex align-start" @onclick="() => ToggleVerse(verse.Number)">
                <MudText Typo="Typo.body1" Class="mr-3 font-weight-medium">@verse.Number</MudText>
                <div class="flex-grow-1">@(new MarkupString(verse.Html))</div>
            </div>
        </MudPaper>
    }
</MudContainer>

<!-- Hidden audio element for playback (always in DOM) -->
<audio id="segmentEditorAudioPlayer"
       class="w-100"
       style="max-height: 40px;">
    <source id="segmentEditorAudioPlayerSource" src="" type="audio/mpeg" />
</audio>
```

### Verse toggle logic

- Click a verse → toggle it in `_selectedVerses` (`HashSet<int>` of verse numbers).
- After each toggle, recompute contiguous range: `VerseStart = Min(selected)`, `VerseEnd = Max(selected)`.
- If selection is empty, range is undefined (create mode).
- Invariant: the segment is defined by its range endpoints; every verse in the range is part of the segment. Selecting verse 5 and verse 10 means verses 5-10 are all in the segment (contiguous).

### Save

1. If selection empty in create mode → `MudAlert` warning "Select at least one verse to create a segment" (don't save).
2. If selection empty in edit mode → warn "Segment would be empty — either add verses or delete it" (don't save; user can delete from NavPlaylistMenu instead).
3. Otherwise: build new `Segment` with `VerseStart`, `VerseEnd`, and `Verses` filtered from chapter's verses to the range.
4. Locate `Playlist` in library → find `BookChapter` → if edit mode, replace segment; if create mode, add segment to `BookChapter.Segments`.
5. `PUT /library` with updated `Library`.
6. On success: `Snackbar.Add("Segment saved.", Severity.Success)` → `NavigationManager.NavigateTo("")` (back to root).
7. On failure: log + `Snackbar.Add("Save failed.", Severity.Error)`.

### Cancel

- `NavigationManager.NavigateTo("")` — no persistence.

### Delete (from editor, for consistency)

- Available as a button in the editor's top bar (in addition to the NavPlaylistMenu menu).
- Removes segment from chapter, saves, navigates back.
- Same Snackbar undo-style confirmation.

---

## 3. Playback During Editing

### Why in-progress playback matters

The segment's verse range may have changed (verses added/removed). Playing the *saved* audio segment would play the old verse range, not the new one. Playing the *in-progress* segment means: use the new `VerseStart`/`VerseEnd` to compute the actual audio time range, then play that range from the chapter's audio file.

### How

- Editor has its own `AudioPlayer` instance (new, not shared with NavPlaylistMenu — different page).
- `PlayerId` = `"segmentEditorAudioPlayer"` (unique per editor session).
- `SourceUrl` = chapter's `AudioUrl` (same as NavPlaylistMenu uses).
- `SegmentStart`/`SegmentEnd` = current in-progress segment's verse audio start/end (from selected verses' `AudioStart`/`AudioEnd`), re-computed on each toggle.
- Playback timer, position polling, verse highlight, and scroll — same pattern as NavPlaylistMenu, but using editor's own verse IDs and calling `StateHasChanged()` instead of `_dropContainer.Refresh()`.

### Verse IDs in editor

The `scrollToVerse` JS function looks up `document.getElementById(id)`. Editor renders verses with IDs like `$"verse-{chapterNumber}-{verseNumber}"` — different from NavPlaylistMenu's `$"verse-{chapter.ChapterNumber}-{segment.VerseStart}-{verse.Number}"` (which includes segment start). Different pages, different DOM — fine.

---

## 4. Shared Playback Helper — Extraction Plan

### What to extract from NavPlaylistMenu

| Piece | Why extract | Where it goes |
|---|---|---|
| `AudioPlayer` ref + `OnAfterRenderAsync` JS init | Reusable across pages | Helper (passed in) |
| `StartPlaybackTimer`/`StopPlaybackTimer` | Identical logic | Helper |
| `UpdatePlaybackPosition` (300ms poll → current verse ID) | Identical, except highlight callback differs | Helper; raises callback |
| `PlayCurrentSegment`/`CancelPlaybackAsync`/`SeekAsync`/`PlayAsync`/`PauseAsync` | Delegate to AudioPlayer ref | Helper |
| `_currentPlaybackTime`, `_currentPlayingVerseId`, `_lastScrolledVerseId` | State both pages need | Helper |
| `_playbackTimer` (System.Timers.Timer) | Same | Helper |

### What stays page-specific

| Piece | NavPlaylistMenu | SegmentEditor |
|---|---|---|
| Highlight callback | `_dropContainer.Refresh()` + `StateHasChanged()` | `StateHasChanged()` only |
| Verse ID format | `$"verse-{chapter.ChapterNumber}-{segment.VerseStart}-{verse.Number}"` | `$"verse-{chapterNumber}-{verseNumber}"` |
| Audio source URL resolution | Chapter's `AudioUrl` from inflated BookChapter | Chapter's `AudioUrl` fetched from Version endpoint |
| Segment start/end | From `CurrentSegment.Value.Segment.Verses` | From selected verse range in editor |
| TTS narration (`speakSequenceAsync`) | NavPlaylistMenu does it on segment play | Editor may omit on first pass |

### Extraction approach for this sprint

**Option C (pragmatic):** Create a `SegmentPlaybackHelper` class that owns the timer, position poll, and verse-ID resolution. The helper:
- Does NOT own the `AudioPlayer` ref (passed in by the page, since the `<audio>` element lives in each page's markup).
- Does NOT touch the DOM.
- Exposes: `CurrentVerseId`, `PlaybackTime` (properties the page can poll), plus `PlaySegment(AudioPlayer player, string chapterAudioUrl, decimal segmentStart, decimal segmentEnd)`, `Pause()`, `Seek(decimal time)`, `Cancel()`.
- Fires `Action<string> OnCurrentVerseIdChanged` callback when the current verse changes.

**NavPlaylistMenu:** wraps the helper with its drop-container refresh.  
**SegmentEditor:** wraps the helper with `StateHasChanged()`.

**Refactor to Option A (abstract base class) in a follow-up** once both pages are using it and the pattern is proven.

### Helper location

`BiblePlaylist/Shared/SegmentPlaybackHelper.cs` (Shared project) — so both Client and Server could reference it if needed, though initially only Client uses it. Alternatively `BiblePlaylist/Client/SegmentPlaybackHelper.cs` if Shared isn't appropriate. I'll put it in Client since it's UI playback logic specific to the Blazor client.

---

## 5. MudMenu + MudMenuItem — Exact API

From source docs (`MenuIconButtonsExample.razor`):

```razor
<MudMenu Icon="@Icons.Material.Filled.MoreVert"
         AriaLabel="Open user menu">
    <MudMenuItem Label="Profile" />
    <MudMenuItem Label="My account" />
    <MudMenuItem Label="Logout" />
</MudMenu>
```

### Parameters used

- `Icon` — `Icons.Material.Filled.MoreVert` (triple dots)
- `AriaLabel` — "Segment options" (accessibility)
- `Label` on each `MudMenuItem` — "Edit segment", "Delete segment"
- `Color` on delete item — `Color.Error` (visual cue for destructive action)

No two-way binding needed — menu is purely click-driven. `MudMenu` opens on click of its icon; `MudMenuItem` handles its own click.

---

## 6. Routing + Query Parameter Parsing

### Primary: `[SupplyParameterFromQuery]` (standard Blazor .NET 6+, available in .NET 10)

```razor
@code {
    [Parameter] [SupplyParameterFromQuery] public int Book { get; set; }
    [Parameter] [SupplyParameterFromQuery] public int Chapter { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string Playlist { get; set; }
    [Parameter] [SupplyParameterFromQuery] public int SegmentStart { get; set; }
    [Parameter] [SupplyParameterFromQuery] public int SegmentEnd { get; set; }
}
```

### Fallback: manual parse from `NavigationManager.Uri`

```csharp
var uri = new Uri(NavigationManager.Uri);
var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
int book = int.Parse(query["book"]);
```

I'll go with primary and fall back if it doesn't compile cleanly.

### Editor URL examples

- **Edit mode:** `/edit-segment?book=43&chapter=3&playlist=library_greg_hardin_live_com&segmentStart=5&segmentEnd=10`
- **Create mode:** `/edit-segment?book=43&chapter=3&playlist=library_greg_hardin_live_com`

### Future URL-share shape (your plan)

`/playlists?deflate=<compressed-string>` — different route, separate concern. `/edit-segment` route stays stable.

---

## 7. Commit Plan — Small, Rollback-Safe

| Commit | Content | Risk if broken |
|---|---|---|
| 1 | `NavigationManager` injection + `MudMenu` triple-dots on segment cards in NavPlaylistMenu + "Edit segment" / "Delete segment" menu items. Delete is wired (Snackbar confirmation + re-add segment). | Missing menu; editor page doesn't exist yet (navigate-to does nothing) — safe |
| 2 | `Pages/SegmentEditor.razor` shell — renders chapter verses, shows selection state, Save/Cancel buttons (Save/Cancel navigate back without persistence). `SegmentPlaybackHelper.cs` extracted. | Editor page exists but doesn't save — harmless |
| 3 | Verse selection logic in editor — click toggles selection, range recomputes, segment display updates. | Selection works but doesn't persist — harmless |
| 4 | Save + Delete persistence — builds Segment, updates Library, PUT `/library`, Snackbar, navigate back. Delete from editor top bar. | Save may have a bug — rollback commit 4 only |
| 5 | Playback during editing — AudioPlayer in editor, segment start/end from selection, highlight + scroll, Play/Stop button. | Playback may have a bug — rollback commit 5 only |

Commits 1-3 are display/navigation only (no persistence, no playback) — very low risk. Commits 4-5 touch persistence and playback — the riskier bits, each in its own commit.

---

## 8. Test Plan

### Existing test setup

- `BiblePlaylist.Tests` uses Bunit 1.40.0 + Moq + xunit.
- `MudBlazor9MigrationTests.cs` already uses `TestContext` with `AddMudBlazorDialog()` + `AddMudServices()`.
- New tests go in the same test project.

### New tests

| Test | What it verifies |
|---|---|
| `Editor_RendersChapterVerses` | Given a chapter with verses, the editor renders them with verse numbers and HTML content. |
| `Editor_EditMode_ShowsSelectedVerses` | Given an existing segment (verse range 5-10), verses 5-10 are highlighted on load. |
| `Editor_ToggleVerse_AddsToSelection` | Clicking a verse outside the selection adds it; range expands contiguously. |
| `Editor_ToggleVerse_RemovesFromSelection` | Clicking a verse inside the selection removes it; range shrinks. |
| `Editor_Save_SegmentUpdated` | Save builds the segment with correct `VerseStart`/`VerseEnd`/`Verses`; PUT `/library` is called with updated library. (Mock `HttpClient`.) |
| `Editor_Cancel_NoChangePersisted` | Cancel navigates back without calling PUT. |
| `Editor_DeleteSegment_SegmentRemoved` | Delete removes the segment from the chapter; PUT is called; Snackbar shown. (Mock `HttpClient` + verify Snackbar.) |
| `Editor_Playback_HighlightsCurrentVerse` | Playing the segment highlights the current verse and calls `scrollToVerse` (verify JS interop invocation). |

### Bunit + MudBlazor + JS interop notes

- Bunit can render MudBlazor components with `AddMudServices()` + `AddMudBlazorDialog()`.
- JS interop in Bunit: use `MockJSInterop` or `ctx.JSInterop.Setup` to capture invocations. For playback highlight tests, I'll assert that `scrollToVerse` was invoked with the expected verse ID.
- MudBlazor menu interactions in Bunit may be limited (menu open/close is JS-driven). I'll test the menu's existence and the menu item's callback wiring; manual verification via the live preview for menu open/close behavior.

---

## 9. Environmental Dependencies — What I May Need From You

### Things I can self-serve

- `dotnet build` — verify compilation after each commit.
- `dotnet test` — run the test suite (including new tests) after each commit.
- `dotnet run` / `dotnet watch` on the Client project — preview localhost.
- Reading source files, searching code, patching files — all via tools.

### Things I'll flag if I hit a wall

- **MudBlazor package availability:** The project references MudBlazor 9.8.0 from NuGet. If the build needs to restore packages and there's a network issue, I'll flag it.
- **`SupplyParameterFromQuery` availability:** If the parameter binding doesn't work in your project setup for some reason (e.g. namespace issues), I'll fall back to manual query parsing — no need for you to do anything.
- **JS interop verification:** I'll read `audioplayer.js` (done) and verify the functions exist. If the editor's playback doesn't work in the live preview, I'll need you to confirm the `<audio>` element is rendering and the JS functions are being called — I can check via browser dev tools in the preview pane.
- **Bunit JS interop mocks:** If Bunit's JS interop mocking for the menu or playback doesn't work as expected, I'll simplify the tests to what Bunit can verify and flag manual testing for the rest.

### No environment tweaks needed upfront

I don't foresee needing you to install anything, change configs, or modify the environment. The project is self-contained with its `.sln`, `.csproj` files, NuGet references, and test project. If I hit a build or test issue that requires environment changes (e.g. a missing package, a permission issue, a port conflict), I'll ask before making changes.

---

## 10. Reference URLs (Stored in Honcho)

- **MudBlazor source docs (dev branch):** `https://github.com/MudBlazor/MudBlazor/tree/dev/src/MudBlazor.Docs/Pages/Components`
- **Raw component page + examples:** `https://raw.githubusercontent.com/MudBlazor/MudBlazor/dev/src/MudBlazor.Docs/Pages/Components/<Component>/<File>.razor`
- **Components used by this plan:** Dialog, Button, Fab (ButtonFab), Grid, ToggleIconButton, DropZone (MudDropContainer + MudDropZone), Menu
- **MudBlazor API reference:** `https://mudblazor.com/api/<Component>`
- **Microsoft Learn MCP tools available:** `microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search`

---

## 11. Decisions Log

| # | Decision | Rationale |
|---|---|---|
| 1 | Contiguous range only | User confirmed. Non-contiguous → separate segment. |
| 2 | Create + edit in same page; create minimal on first pass | User preference. Page scaffolds for both; create is trivial empty-selection → save. |
| 3 | Playback during editing uses in-progress segment | User requested. Plays the new verse range, not the saved one. |
| 4 | Query-string routing | User preference. Future URL-share plans use a different route. |
| 5 | Delete = Snackbar undo-style, no dialog | User confirmed. Segments are easy to recreate. |
| 6 | Option C (helper class) for playback extraction on first sprint | Avoids touching NavPlaylistMenu's inheritance on the first commit. Refactor to base class later. |
| 7 | `[SupplyParameterFromQuery]` primary, manual parse fallback | Cleanest Blazor pattern; fallback if it doesn't work. |
| 8 | Five small commits | Rollback-safe. Each commit is a logical unit. |
| 9 | MudMenu triple-dots icon = `Icons.Material.Filled.MoreVert` | Standard MudBlazor pattern (verified in source: `MenuIconButtonsExample.razor`). |
| 10 | Editor verse IDs = `verse-{chapterNumber}-{verseNumber}` | Different from NavPlaylistMenu's format (which includes segment start). Different pages, different DOM — fine. |
