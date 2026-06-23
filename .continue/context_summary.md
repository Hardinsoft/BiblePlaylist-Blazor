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

## Meta-Summary: How to Use the Robust PowerShell Append Rule\n\nThis rule must be used whenever appending content that is:\n1. Multi-line (contains line breaks).\n2. Contains special characters (e.g., quotes like \" or shell operators like &).\n3. Is complex enough that simple single-line Appends might fail.\n\n### The Mandatory 3-Step Workflow:\n\n**STEP 1: Create Temp File:** Use `create_new_file` to write the entire content (the story, summary, etc.) into a unique temporary path (e.g., `.continue/temp_[GUID].md`).\n\n**STEP 2: Append Content:** Run PowerShell via `run_terminal_command`: \`Get-Content '.continue/temp_[GUID].md' | Add-Content '.continue/context_summary.md' -Encoding utf8\`.\n\n**STEP 3: Clean Up:** Finally, run PowerShell to delete the temporary file: \`Remove-Item '.continue/temp_[GUID].md' -Force\`.\n\n### Critical Instruction:\nAlways generate a unique filename in Step 1. Do not reuse temp filenames."
