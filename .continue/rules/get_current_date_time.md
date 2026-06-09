---
name: Use run_terminal_command for current date/time on Windows
description: Forces the agent to query the live Windows system clock via the terminal tool instead of hallucinating the date or time.
---

**MANDATORY BEHAVIOR FOR DATE / TIME QUERIES**

Whenever the user asks for the current date, time, datetime, "what time is it", or when you need an accurate real-time timestamp (for logs, filenames, commit messages, scheduling, debugging output, etc.), you **MUST** follow these steps **exactly**:

1. **Call the `run_terminal_command` tool** (do not answer from knowledge).
2. Use one of the following Windows commands (try them in this order of preference):

   **Preferred (PowerShell – highest precision):**
   ```powershell
   powershell.exe -NoProfile -Command "Get-Date -Format 'yyyy-MM-dd HH:mm:ss K'"
   Fallback:
   ```powershell
   powershell.exe -NoProfile -Command "Get-Date"
   ```cmd
   cmd.exe /c "echo %DATE% %TIME%"
3. After the tool returns the result, format your response exactly like this:

Desired Response Format
First line (raw system time):
Today's date and time, according to the system clock, is 2026-06-07 23:33:09 -07:00.
Then immediately follow with the plain English translation:
The date and time 2026-06-07 23:33:09 -07:00 translates to:
June 7th, 2026, at 11:33 PM (23:33) in the time zone seven hours behind Coordinated Universal Time (UTC-7).