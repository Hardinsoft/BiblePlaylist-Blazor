---
name: Append file
description: Uses create_new_file tool to write content to a temp file, then uses run_terminal_command with PowerShell to append it and clean up. Avoids all quoting/escaping issues with complex or multi-line content.
alwaysApply: true
---

When you need to append content to `.continue/context_summary.md` (or any file), follow this exact workflow to avoid quoting and escaping problems:

### Step-by-step Process (MANDATORY for multi-line or complex content):

1. **Create a temporary file** using the `create_new_file` tool.
   - Path: `.continue/temp_{unique}.md` (generate a unique identifier, e.g. `temp_20240614_2145_a3f9.md` or use part of a GUID).
   - Write the full content the user requested into this temp file.

2. **Append the temp file to the target** using `run_terminal_command` with PowerShell:
   ```powershell
   Get-Content '.continue\temp_20240614_2145_a3f9.md' | Add-Content '.continue\context_summary.md' -Encoding utf8
   ```

3. **Delete the temporary file** using `run_terminal_command`:
   ```powershell
   Remove-Item '.continue\temp_20240614_2145_a3f9.md' -Force
   ```

### Rules the model must follow:

- When appending the content add a paragraph break so that the newly appended content will not be combined with existing content.
- For any content that is multi-line, contains special characters, quotes, or is longer than one simple line → **always use this temp file workflow**.
- Never try to pass multi-line content directly via `powershell.exe -Command` or `echo`.
- Always use a unique temp filename (include date/time or a short random string/GUID).
- Use `Get-Content ... | Add-Content ...` (PowerShell) for the append step as requested.
- Always clean up the temp file after the append succeeds.
- For very simple single-line appends, you may still use a direct `Add-Content` command, but prefer the temp file method when in doubt.

### Example of correct sequence:

- First call `create_new_file` with path `.continue/temp_abc123.md` containing the full text.
- Then call `run_terminal_command` with the `Get-Content | Add-Content` command.
- Then call `run_terminal_command` again with `Remove-Item` to delete the temp file.

