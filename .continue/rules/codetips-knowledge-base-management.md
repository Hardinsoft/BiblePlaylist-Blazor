---
globs: "*.cs, *.razor, *.js"

description: Manages the lifecycle and retrieval of supplemental, educational
  code samples stored in the CodeTips GitHub repository.
alwaysApply: false
---

When managing the CodeTips repository, treat it as a curated library of code snippets. If asked to add or retrieve code, use the appropriate GitHub tool calls (e.g., gh_create_or_update_file or gh_get_file_contents) to manage the samples at `ocdsoft/continue-memory/BiblePlaylist/codetips-knowlegde-base.md`.  Always confirm with the user if the samples are new, and when creating an issue, specify that the code is a 'Sample/Example' and not a required feature fix.