---
globs: .continue/_context_summary.md
description: This rule handles the persistence of the conversation state by
  summarizing the chat history and saving it to a designated context file,
  ensuring the user is notified of the successful update.
alwaysApply: false
---

When explicitly instructed to "add to context history," you must perform the following steps: 1) Generate a comprehensive summary of the entire current chat history. 2) Use the edit_existing_file tool to append this summary to the file located at `.continue/_context_summary.md`. 3) After the file edit is confirmed, you must notify the user in the chat with a confirmation message that includes the generated summary of what was recorded in the file. 4) Ensure that the summary appended to the file *does not* contain a summary of the summary itself.