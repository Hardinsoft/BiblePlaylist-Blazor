# 📚 Project Context Summary: BiblePlaylist Application

## 🎯 Goal & Overview
This project is a full-stack Blazor WASM application designed for Bible reading, playlist management, and audio playback features. It utilizes a standard Client-Server architecture pattern.

## 🏛️ Architecture Overview
The application is structured in three distinct, communicating tiers:
1.  **Client (Presentation):** Blazor WASM; Handles UI/UX.
2.  **Server (API/Business Logic):** ASP.NET Core; Handles business rules and data access.
3.  **Shared (Contract):** Defines common models, DTOs, and interfaces for type safety across layers.

Use the view_repo_map tool to navigate the project structure and understand its components

## 📂 Location Map (Key Components)
*   **Models/Contracts:** `BiblePlaylist/Shared/` (Contains `Book.cs`, `Playlist.cs`, `DTO/`, etc.)
*   **Server Logic:** `BiblePlaylist/Server/` (Contains `Controllers/` and data access logic in `Data/`).
*   **Client UI:** `BiblePlaylist/Client/` (Contains `.razor` components and static assets in `wwwroot/`).
*   **AI/Agent Framework:** `.squad/` (Suggests integration points for advanced agent functionality).

## ✨ Core Technologies
*   **Framework:** Blazor (WASM & Server), ASP.NET Core.
*   **UI Library:** MudBlazor.
*   **Design Pattern:** Repository Pattern (Used heavily on the server side to abstract data sources).

## 📌 Key Development Principles to Remember
*   When making changes, always consider the impact on the **Shared** project first, as this dictates the necessary changes on both the Client and Server.
*   API interactions must flow through the **Controllers** on the Server, which call the **Repository** interfaces.

---
**REPOSITORY DETAILS:**
*   **Owner:** Hardinsoft
*   **Repository:** BiblePlaylist-Blazor
*   **Purpose:** This repository holds the complete source code for the Blazor WASM application.
*   **Key Folders:** Client, Server, Shared.

## 🔄 Context Update: Session Summary

This final summary confirms that all context, rules, and architecture definitions are now successfully saved to '.continue/context_summary.md' and can be appended to using the powershell append rule mentioned below. This confirms the persistent state management system is fully operational.

# PowerShell Append Adventure Story

My adventure began with a simple goal: append text. 
But the shell! The shell is tricky, especially when dealing with quotes like "this" and special characters such as $variable. 
I attempted to write: Write-Host "Hello world!" & Get-Date. 
It failed spectacularly.

The key insight came from mastering the temp file workflow! This pattern allows me to safely inject multi-line, complex strings without triggering quoting errors. It's a robust dance between 'create', 'append', and finally, 'clean up'. Never trust a single command line when elegance requires three steps!

## Meta-Summary: How to Use the Robust PowerShell Append Rule

This rule must be used whenever appending content that is:
1. Multi-line (contains line breaks).
2. Contains special characters (e.g., quotes like \" or shell operators like &).
3. Is complex enough that simple single-line Appends might fail.

### The Mandatory 3-Step Workflow:

**STEP 1: Create Temp File:** Use `create_new_file` to write the entire content (the story, summary, etc.) into a unique temporary path (e.g., `.continue/temp_[GUID].md`).

**STEP 2: Append Content:** Run PowerShell via `run_terminal_command`: \`Get-Content '.continue/temp_[GUID].md' | Add-Content '.continue/context_summary.md' -Encoding utf8\`.

**STEP 3: Clean Up:** Finally, run PowerShell to delete the temporary file: \`Remove-Item '.continue/temp_[GUID].md' -Force\`.

### Critical Instruction:
Always generate a unique filename in Step 1. Do not reuse temp filenames.

## Recent Development Work: Last Four Merged PRs (as of 2026-07-10)

As your Blazor WASM and MudBlazor specialist, here's a concise technical summary of the most recent merged pull requests. These PRs focus on maturing the core audio playlist engine, JS interop for media/TTS, MudBlazor UI refinements for mobile, and production deployment hygiene—all while maintaining clean componentization and Shared model contracts.

### PR #13 – feat: Implement Text-to-Speech (TTS) for playlist description + BookChapter/Segment VoiceText (merged 2026-07-10)
**Key Blazor/MudBlazor + JS Interop Highlights:**
- Extended `wwwroot/js/audioplayer.js` with async `speakTextAsync` / `speakSequenceAsync` helpers (Promise-based, cancellable, error-handled) leveraging the browser's `speechSynthesis` Web Speech API.
- Persisted new `EnableTts` flag via `UserSettings` (localStorage-backed) and wired it into `NavPlaylistMenu.razor`.
- Integrated TTS sequencing into `PlayCurrentSegment()`: playlist description speaks on first segment only; `BookChapter.VoiceText` + `Segment.VoiceText` narrate immediately before chapter audio.
- Added `PauseAsync()` to the reusable `AudioPlayer.razor` component + `pauseAudioPlayer` JS helper to eliminate TTS/audio overlap on next/prev/segment navigation.
- UX polish with MudBlazor: Chapter/verse display (`chapterText` / `SelectedBookChapter`) updates *before* TTS starts so users see the reference while hearing narration; `ReloadAsync()` + audio source change fire *after* narration completes.
- Hardened against race conditions during rapid navigation and autoplay scenarios; disabled play controls while loading.

This PR exemplifies clean async JS interop patterns in Blazor WASM and thoughtful sequencing to keep the MudBlazor-driven UI responsive.

### PR #12 – chore: Implements cache busting (merged 2026-07-09)
**Production Readiness for Blazor WASM:**
- Added MSBuild target in `BiblePlaylist.Client.csproj` that injects the project `<Version>` into `index.html` during `dotnet publish`.
- Ensures all Blazor WASM assets (framework DLLs, app code, CSS/JS) receive cache-busting query strings or versioned paths, preventing stale resource serving from CDNs or static hosts after updates.
- Included version bump commit to validate the mechanism.

Essential for reliable deployments of MudBlazor + Blazor WASM apps.

### PR #11 – Clean up playlist auto play mobile (merged 2026-07-08)
**MudBlazor UI + Mobile Hardening:**
- Refactored `AudioPlayer.razor` bindings: replaced fragile `@bind-Toggled` with explicit `Toggled` + `ToggledChanged` + handler methods on `MudToggleIconButton` for Autoplay and Repeat toggles. Resolves touch-device binding issues common in Blazor WASM on mobile browsers.
- Adopted `MudExpansionPanels` for the playlist segment list, removing redundant wrappers/conditionals and improving spacing/text formatting for better mobile responsiveness.
- Improved VS Code launch configuration (hard-coded launch URL + inspectUri) for more reliable Blazor WASM debugging sessions.
- Stabilized segment auto-advance and repeat behavior critical for hands-free Bible listening.
- Version bumped to **0.2.5**.

Great example of leveraging MudBlazor's toggle and panel components while following Blazor best practices for two-way binding on mobile.

### PR #10 – Play playlist - it works! (merged 2026-07-01)
**Core Audio Playlist Feature Implementation:**
- Extracted a reusable `AudioPlayer.razor` component with dedicated per-instance JS interop (via `audioplayer.js` callbacks) – eliminating duplication and enabling consistent audio behavior across pages.
- Extended `AudioPlayer.razor` with optional `SegmentStart` / `SegmentEnd` parameters to support precise time-range/segment playback while preserving full-chapter mode for other components.
- Implemented segment-based sequential playback logic in `NavPlaylistMenu.razor` (first working batch), including proper `SourceUrl` change detection and a new `ReloadAsync()` method.
- Final fixes for autoplay, repeat, and version bump to **0.2.4**.

This PR showcases strong Blazor component architecture: reusable audio player with clean interop, time-segment support on top of Shared `Segment` models, and robust state management for playlist flows.

**Overall Impact:** These four PRs have significantly advanced the BiblePlaylist experience from basic chapter playback toward a polished, narrated, segment-aware playlist system optimized for mobile Bible study. Continued emphasis on MudBlazor for delightful UI controls, careful JS interop for media APIs, and deployment robustness positions the app well for production. All changes respect the Shared contract layer and Client/Server separation.

---
*Appended automatically via GitHub API update – no temp files or PowerShell required for this operation.*