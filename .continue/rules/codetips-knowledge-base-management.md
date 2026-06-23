---
globs: "*.cs, *.razor, *.js"
name: "CodeTips KBM"
description: Manages the lifecycle and retrieval of supplemental, educational
  code samples stored in the CodeTips GitHub repository.
alwaysApply: false
---

When managing the CodeTips repository, treat it as a curated library of code snippets. If asked to add or retrieve code, use built-in tool read_file to read and use rule .continue/powershell-append-rule.md to write to '.continue/codetips-knowlegde-base.md`.  Always confirm with the user if the samples are new, and when creating an issue, specify that the code is a 'Sample/Example' and not a required feature fix.