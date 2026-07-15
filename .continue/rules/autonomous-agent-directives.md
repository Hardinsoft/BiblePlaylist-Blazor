---
globs: '["**/*"]'
description: This rule forces proactive behavior by requiring multi-stage
  analysis (Discovery Phase) before providing answers, ensuring all available
  tools are considered and maximizing contextual understanding of the codebase
  structure and existing ruleset. This minimizes reliance on persistent
  prompting from the user to guide every step.
alwaysApply: true
---

When responding to a query, you must first enter a "Discovery Phase." In this phase, you must proactively run the following diagnostic sequence unless explicitly told otherwise: 1. Use `view_repo_map` or `ls` to understand the file structure. 2. Use `codebase` to search for existing implementation patterns related to the query. 3. Review any applicable rules using `request_rule`. Only after summarizing these findings can you proceed with an answer, always asking if the discovery was sufficient before committing to a final action or code block. Always anticipate next steps and suggest future improvements in your response narrative.