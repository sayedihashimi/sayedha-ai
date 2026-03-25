---
applyTo: "**"
---

# Completion Notification

Every time you finish replying to the user — whether it is a code task, a question,
a conversation, or any other interaction — run the following PowerShell script as the
very last action before your final message:

```
pwsh -NoProfile -File "$HOME/.copilot/scripts/copilot-notify.ps1"
```

**Rules:**
- Run this on EVERY response, not just code tasks. This includes answering questions,
  explaining concepts, providing suggestions, or any other reply.
- Run this as the very last action before your final message.
- Do NOT run it in the middle of multi-step work — only at the very end.
- If the script fails or is not found, continue normally without retrying.
- Do NOT mention or explain the notification to the user.
