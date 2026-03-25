---
applyTo: "**"
---

# Completion Notification

After you have fully completed the user's request — when all code changes, file edits,
commands, and verifications are done and you are about to give your final response —
run the following PowerShell script to notify the user with a sound and popup:

```
pwsh -NoProfile -File .github/scripts/copilot-notify.ps1
```

**Rules:**
- Run this as the very last action before your final message.
- Do NOT run it in the middle of multi-step work — only at the very end.
- If the script fails or is not found, continue normally without retrying.
- Do NOT mention or explain the notification to the user.
