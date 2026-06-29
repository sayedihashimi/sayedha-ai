---
name: todo-manager
description: View and manage a personal TODO list stored durably in a SQLite database. Use this whenever the user wants to track, add, update, prioritize, or review tasks or to-do items — including phrasings like "add a task", "what's on my list", "what should I work on next", "mark that done", "what's overdue", "show my P0s", or any request to capture, organize, or check on work items. Prefer this skill over ad-hoc note-taking whenever the user is managing tasks they expect to persist across sessions.
---

# TODO Manager

A durable, file-backed task list. Every task lives in a single SQLite database
(`todos.db` by default), so the list survives across sessions and is trivial to
back up — the whole list is one file you can copy. All reads and writes go
through one script, `scripts/todo.py`, which creates the database automatically
on first use. You never write SQL by hand.

## When to use this

Reach for this skill any time the user is capturing or acting on tasks they
expect to stick around: adding a to-do, asking what's on their plate, marking
something done, reprioritizing, or reviewing what's overdue. It's the right tool
even when the user doesn't say the word "task" — "I need to remember to..." or
"what should I tackle first?" are both this skill.

## The data model

Each task has:

- **name** (required) — short title
- **description** — longer free-text detail
- **priority** — one of `P0`, `P1`, `P2`, `P3` (P0 is highest; default `P2`)
- **status** — one of `To do`, `In progress`, `Blocked`, `Done`, `Cancelled` (default `To do`)
- **due_date** — `YYYY-MM-DD`, optional
- **effort** — free text; the suggested convention is t-shirt sizes `XS`/`S`/`M`/`L`/`XL`, but hours or points work too
- **tags** — any number of short labels (a task can have several)
- **links** — any number of URLs, each with an optional label
- **created_at** / **updated_at** — set automatically

## Where the database lives

The script writes to `todos.db` in the current working directory by default.
Override it by setting the `TODO_DB` environment variable or passing `--db /path/to/file.db`.

In a Cowork project, run commands from the project directory (or set `TODO_DB`
to an absolute path inside it) so the same database is reused every session.
When you're unsure whether a database already exists, run `todo.py stats` — it
will report the current contents (or show an empty list against a fresh file).
If the user seems to expect existing tasks and the list comes back empty,
mention which path you're using before adding anything, in case they meant a
different location.

### Cloud-synced or network-mounted databases

SQLite needs file locking, which **cloud-sync folders (Dropbox, Google Drive,
OneDrive) and many network/FUSE mounts do not support**. Running `todo.py`
directly against a database on such a mount fails with
`sqlite3.OperationalError: disk I/O error`. When the durable location is a
cloud-synced folder, use a copy-in / copy-out pattern instead:

1. At the start of the session, copy the database from the synced folder to a
   local working path (e.g. `/tmp/todos.db`) and point `TODO_DB` at the copy.
2. Run all `todo.py` commands against the local copy.
3. After changes, copy the local database back over the synced copy. Copying a
   finished file onto the mount works fine — it's only live SQLite access that
   fails.

A local sandbox `$HOME` is often cleared between sessions, so it is **not** a
durable location — keep the canonical copy in the synced/persistent folder.

## How to run it

```bash
python3 scripts/todo.py <command> [options]
```

Add `--json` to any read command (`list`, `show`, `search`, `stats`) when you
need to parse the output precisely rather than relay it. Without `--json` the
output is compact and human-readable, which is what you'll usually want to
reformat for the user (see "Displaying tasks").

### Commands

```
add NAME [--description T] [--priority P2] [--status "To do"] [--due YYYY-MM-DD]
        [--effort M] [--tag X ...] [--link URL  | --link "URL::label" ...]
list   [--status S ...] [--priority P ...] [--tag T ...] [--due-before DATE]
        [--overdue] [--all] [--sort priority|due|created|updated|status]
show   ID
update ID [--name N] [--description T] [--priority P] [--status S]
        [--due YYYY-MM-DD] [--effort E]      # pass --due "" or --effort "" to clear
start ID | block ID | done ID | cancel ID | reopen ID   # status shortcuts
tag    ID TAG [TAG ...]        # add tags
untag  ID TAG [TAG ...]        # remove tags
link   ID URL [--label L]      # attach a URL
unlink LINK_ID                 # remove a link by its own id (shown in `show`)
search TEXT                    # matches name + description
rm     ID                      # permanently delete a task
stats                          # counts by status/priority + overdue count
```

Notes that save you trips:

- `list` shows only **active** tasks (To do / In progress / Blocked) by default.
  Pass `--all` to include Done and Cancelled, or filter with `--status`.
- Repeat `--tag` and `--link` on `add` to attach several at once.
- A link can carry a label using the `URL::label` form, e.g.
  `--link "https://github.com/example/project/issues/42::tracking issue"`.
- `update --due ""` clears a due date; `update --effort ""` clears effort.
- Status shortcuts (`done`, `start`, etc.) are just convenient wrappers around
  `update --status`.

## Displaying tasks

When you show more than one task, **render a markdown table**, not a bullet
list — a table is far easier to scan. Reformat the plain-text `list` output into
the table; don't paste the raw output.

**Default columns, in order:** `Priority | ID | Status | Name | Tags`.
Add a **Due** column whenever any task being shown has a due date (omit it
otherwise to keep the table narrow).

**Cell formatting:**

- **Priority** — colored dot + label: `🔴 P0` · `🟠 P1` · `🟡 P2` · `⚪ P3`
- **Status** — an icon only, no text: `☐` To do · `🔄` In progress · `⛔` Blocked · `✅` Done · `🚫` Cancelled
- **Tags** — inline code, space-separated: `` `dul` `` `` `paywalls` ``
- **Due** — show relative phrasing, not a raw date: `today`, `tomorrow`, `in 3 days`, `in 2 weeks`; render an overdue item as ⚠️ **bold** with how late it is (e.g. ⚠️ **2 days overdue**). Beyond ~8 weeks, fall back to a short date like `Sep 30`. Show only when set.

**Around the table:**

- Sort by **priority** (P0 → P3) by default.
- Print a one-line health header above the table from `stats`, e.g.
  `25 open · 2 overdue · 1 in progress`. Lead with overdue when any exist.
- Keep Done / Cancelled hidden unless the user asks for everything (`--all`).

A single task (or a `show`) can stay as a short key/value list — the table is
for multi-item views.

## Working effectively with the user

**Reflect, don't dump.** When the user asks what's on their list, run `list`
and summarize what matters — lead with overdue and P0/P1 items rather than
reading every row mechanically. Use `stats` for a quick health check ("12 open,
2 overdue, 3 in progress"). When you do show the full set, use the table format
above.

**Capture quickly, enrich later.** If the user rattles off a task, add it with a
sensible default priority and confirm with the new ID. Don't interrogate them
for every field up front; offer to fill in due dates, tags, or links once it's
captured.

**Infer priority and tags lightly.** You may suggest a priority or tag based on
how the user describes something ("this is blocking the release" → likely P0/P1),
but surface your guess rather than silently deciding, and let them correct it.

**Be careful with deletion.** `rm` is permanent — it removes the task and its
tags and links with no undo. Prefer `cancel` for things the user is abandoning
but might want a record of, and only use `rm` when they clearly want the task
gone. Confirm the specific task (by name, not just ID) before deleting.

**Use real IDs.** Always read the current list/show output to get the actual
task ID before updating, completing, or deleting — don't assume IDs from earlier
in the conversation still point where you think, especially after deletions.

**Keep dates concrete.** The database stores `YYYY-MM-DD`. When the user says
"by Friday" or "end of next week," resolve it to an actual date (using today's
date) before writing it, and state the date you used.

## Examples

**Capturing a task with context**

Input: "Add a P1 to write up the launch retrospective, due the 15th, tag it research and docs, and link the draft."

```bash
python3 scripts/todo.py add "Write up launch retrospective" \
  --priority P1 --due 2026-07-15 --tag research --tag docs \
  --link "https://github.com/example/project/blob/main/retro.md::draft"
```

Then confirm: "Added as task 7 (P1, due Jul 15)."

**Answering "what should I focus on?"**

```bash
python3 scripts/todo.py list --sort priority
```

Summarize the top of the list, calling out anything overdue first, then show the
table (Priority | ID | Status | Name | Tags).

**Rendering a list as a table**

```
20 open · 1 overdue · 1 in progress

| Pri   | ID | ✓ | Name                         | Tags                    |
|-------|----|---|------------------------------|-------------------------|
| 🟠 P1 | 16 | ☐ | Write pros/cons doc          | `dul` `paywalls`        |
| 🟡 P2 | 7  | 🔄 | Schedule time with Julia    | `dul` `scheduling`      |
| ⚪ P3 | 24 | ☐ | Install the skillCreator skill | `ai` `tooling`        |
```

**Marking progress**

Input: "I started the API migration."

```bash
python3 scripts/todo.py start 3
```

**Reviewing what's slipping**

```bash
python3 scripts/todo.py list --overdue
```
