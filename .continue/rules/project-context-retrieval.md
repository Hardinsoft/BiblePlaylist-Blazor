---
globs: project_summary.md, context_summary.md
name: Project Context Summary
description: A rule to consolidate and present the current state of the
  project's architectural documentation and recent session history from
  dedicated summary files.
alwaysApply: false
---

When prompted to review or retrieve the 'Project Context Summary' or 'Context History', first use the use built-in tool read_file to read both `.continue/project_summary.md` and `.continue/context_summary.md`. Then, synthesize a single, comprehensive report that merges the archiectural overview, the current development status, and the recent conversational updates. Always present this report clearly to the user.