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

## How to run it

```bash
python3 scripts/todo.py <command> [options]
```

Add `--json` to any read command (`list`, `show`, `search`, `stats`) when you
need to parse the output precisely rather than relay it. Without `--json` the
output is compact and human-readable, which is what you'll usually want to
summarize back to the user.

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

## Working effectively with the user

**Reflect, don't dump.** When the user asks what's on their list, run `list`
and summarize what matters — lead with overdue and P0/P1 items rather than
reading every row mechanically. Use `stats` for a quick health check ("12 open,
2 overdue, 3 in progress").

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

Summarize the top of the list, calling out anything overdue first.

**Marking progress**

Input: "I started the API migration."

```bash
python3 scripts/todo.py start 3
```

**Reviewing what's slipping**

```bash
python3 scripts/todo.py list --overdue
```
