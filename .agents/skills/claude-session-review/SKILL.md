---
name: claude-session-review
description: >-
  Review the user's own Claude Code sessions over a time window and give a themed
  breakdown of what they worked on. Reads the local transcripts in
  ~/.claude/projects with a bundled script, then groups the work by topic rather than
  by folder. Use when the user asks what they worked on, for a weekly recap, a summary
  of their Claude sessions, or "what did I do last week". Arguments: optional window
  such as "7d" (default), "2w", "since 2026-10-20" or "2026-10-20..2026-10-27", plus
  "save" to write the breakdown to a file.
---

# Claude session review

Summarize what the user worked on across their Claude Code sessions in a window of time.

## 1. Pick the window

- Default: the last 7 days, ending now.
- `Nd` / `Nw`: the last N days or weeks.
- `since YYYY-MM-DD`, or `YYYY-MM-DD..YYYY-MM-DD` (end date exclusive).
- If the user names a project or folder, still extract everything, then filter in step 3.

## 2. Extract the digest

Run the bundled script. Write the digest to a temp folder, not into the user's project:

```bash
python -I "<skill-dir>/scripts/extract_sessions.py" --days 7 --output "<temp-dir>/sessions-digest.md"
# or: --since 2026-10-20 --until 2026-10-27
```

`<skill-dir>` is this skill's base directory. On Windows use `$TEMP` or `C:\temp\claude-session-review`.

The digest contains:

- A table of sessions per working folder (multi-prompt and single-prompt counts).
- Each multi-prompt session: title, local time span, the user's prompts (first 9 and the last), and an excerpt of the last assistant reply.
- Single-prompt sessions grouped by folder, with identical prompts collapsed (`×N`).

Timestamps are converted to local time. Tool output, subagent traffic, task notifications and compaction summaries are left out. Pasted blocks are kept, between `[pasted]` markers.

The script redacts likely secrets before it writes anything, replacing each with `[redacted]`. It catches:

- JWTs, including ones cut off mid-token.
- Bearer and basic auth values.
- Known key formats: `sk-`, `ghp_`, `github_pat_`, `glpat-`, `xox?-`, `AKIA`, `AIza`, `ya29.` and `npm_`.
- PEM private keys.
- Values of `*token*`, `*secret*`, `*password*` and `*api_key*` assignments.
- Long random-looking strings. Hex hashes, GUIDs, paths and URL segments are left alone.

The redaction is pattern-based, so treat the digest as sensitive anyway.

Read the whole digest. It can exceed one Read page, so page through it with `offset`.

## 3. Fill gaps only where needed

The prompts usually make the topic clear. When a big session's outcome is unclear (many prompts hidden, vague prompts like "do it"), read more from its transcript: `~/.claude/projects/<folder>/<session-id>*.jsonl`. Pull only user prompts and assistant text, never full tool results. Don't open every session.

Ignore the current session (the one running this skill) and any earlier runs of this skill.

## 4. Build the breakdown

- **Group by theme, not by folder.** One initiative often spans several folders (a notes folder plus a code repo plus demo/test folders). Merge them.
- **Order themes by effort**, which you can judge from the number of sessions and prompts.
- **Batches of single-prompt sessions** that repeat scripted prompts are usually test or eval runs for something being built elsewhere. Attach them to that theme and summarize them in one line. Don't list each one.
- For each theme, list the concrete things done: what was built, decided, drafted, posted or fixed. Name the files, tickets, docs and people involved when the prompts mention them. Include what is still open or pending, if it is clear.
- End with a short "misc / one-off questions" group.
- Lead with a one-line summary: the window, the number of sessions, and how many were test runs.
- Never repeat tokens, keys or other secrets that appear in prompts.
- Say which time zone the dates are in.

Keep it skimmable: a heading per theme, bullets underneath, no filler.

## 5. Save (when asked)

If the user asks to save it, or passed `save`:

- Save into the active feature or workstream folder if one is set in this conversation. Otherwise ask where.
- File name: `YYYY-MM-DD-session-breakdown.md`, using the window's end date. Add Obsidian-style frontmatter: `window`, `generated`, `sessions`.
- Don't save the raw digest unless asked. It contains verbatim prompts.
