---
applyTo: "**"
---

# Completion Notification

Every time you finish replying to the user — whether it is a code task, a question,
a conversation, or any other interaction — run the following PowerShell script as the
very last action after your final message:

```
pwsh -NoProfile -File "$HOME/.copilot/scripts/copilot-notify.ps1" -Title "<project>" -Message "<summary>"
```

Replace the placeholders:
- `<project>`: The name of the current repository or project folder (e.g. "sayedha-ai").
- `<summary>`: A brief 1-line summary (under 80 chars) of what you just completed
  (e.g. "Refactored auth module", "Answered question about strawberry",
  "Fixed 3 test failures"). Keep it concise and human-readable.

**Rules:**
- Run this on EVERY response, not just code tasks. This includes answering questions,
  explaining concepts, providing suggestions, or any other reply.
- Run this as the very last action after your final message.
- Do NOT run it in the middle of multi-step work — only at the very end.
- **Do NOT run this if you are a sub-agent** — i.e., if you were spawned by another agent
  via the `task` tool (explore, task, general-purpose, or code-review agents). Only the
  top-level parent agent that directly receives and responds to the user's message should
  run this notification.
- If the script fails or is not found, continue normally without retrying.
- Do NOT mention or explain the notification to the user.
