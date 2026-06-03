# 📚 Project Context Summary: BiblePlaylist Application

## 🎯 Goal & Overview
This project is a full-stack Blazor WASM application designed for Bible reading, playlist management, and audio playback features. It utilizes a standard Client-Server architecture pattern.

## 🏛️ Architecture Overview
The application is structured in three distinct, communicating tiers:
1.  **Client (Presentation):** Blazor WASM; Handles UI/UX.
2.  **Server (API/Business Logic):** ASP.NET Core; Handles business rules and data access.
3.  **Shared (Contract):** Defines common models, DTOs, and interfaces for type safety across layers.

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
**Reminder for Future Sessions:** Please ask to review the "Project Context Summary" or refer to the file path `.continue/project_summary.md` to refresh my memory on this project's structure and conventions.