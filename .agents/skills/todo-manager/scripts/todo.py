#!/usr/bin/env python3
"""
todo.py — a small, durable TODO manager backed by a single SQLite file.

Everything lives in one .db file (default: ./todos.db, override with the
TODO_DB env var or the global --db flag), so the list survives across
sessions and is easy to back up — just copy the file.

Design notes for whoever (human or model) is reading this:
- Stdlib only (sqlite3, argparse, json). No installs, runs anywhere.
- Schema is created automatically on first use; every command is safe to run
  against a fresh file.
- Read commands (list/show/search/stats) accept --json for exact parsing.
  Without it they print a compact human-readable view that's easy to relay.
- Tags and links are one-to-many (a task can have several of each), stored in
  their own tables rather than crammed into a column, so filtering by tag and
  managing links individually stays clean.
"""

import argparse
import json
import os
import sqlite3
import sys
from datetime import datetime, date

PRIORITIES = ["P0", "P1", "P2", "P3"]            # P0 = highest
STATUSES = ["To do", "In progress", "Blocked", "Done", "Cancelled"]
ACTIVE_STATUSES = ["To do", "In progress", "Blocked"]
# Default effort convention is t-shirt sizing, but the column is free text so
# you can store hours/points/anything if that suits you better.
EFFORT_SUGGESTED = ["XS", "S", "M", "L", "XL"]

PRIORITY_RANK = {p: i for i, p in enumerate(PRIORITIES)}
STATUS_RANK = {s: i for i, s in enumerate(STATUSES)}


# --------------------------------------------------------------------------- #
# Database setup
# --------------------------------------------------------------------------- #
def default_db_path():
    return os.environ.get("TODO_DB", "todos.db")


def connect(db_path):
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA foreign_keys = ON")
    _init_schema(conn)
    return conn


def _init_schema(conn):
    conn.executescript(
        """
        CREATE TABLE IF NOT EXISTS tasks (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            name        TEXT NOT NULL,
            description TEXT,
            priority    TEXT DEFAULT 'P2'
                        CHECK (priority IN ('P0','P1','P2','P3')),
            status      TEXT NOT NULL DEFAULT 'To do'
                        CHECK (status IN ('To do','In progress','Blocked','Done','Cancelled')),
            due_date    TEXT,
            effort      TEXT,
            created_at  TEXT NOT NULL,
            updated_at  TEXT NOT NULL,
            completed_at TEXT
        );

        CREATE TABLE IF NOT EXISTS tags (
            task_id INTEGER NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
            tag     TEXT NOT NULL,
            UNIQUE (task_id, tag)
        );

        CREATE TABLE IF NOT EXISTS links (
            id      INTEGER PRIMARY KEY AUTOINCREMENT,
            task_id INTEGER NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
            url     TEXT NOT NULL,
            label   TEXT
        );

        CREATE INDEX IF NOT EXISTS idx_tasks_status   ON tasks(status);
        CREATE INDEX IF NOT EXISTS idx_tasks_priority ON tasks(priority);
        CREATE INDEX IF NOT EXISTS idx_tags_tag       ON tags(tag);
        """
    )
    _migrate(conn)
    conn.commit()


def _migrate(conn):
    """Bring older databases up to the current schema, non-destructively."""
    cols = {r["name"] for r in conn.execute("PRAGMA table_info(tasks)")}
    if "completed_at" not in cols:
        # Existing DB created before completion tracking existed.
        conn.execute("ALTER TABLE tasks ADD COLUMN completed_at TEXT")
    # Backfill: any task already marked Done but missing a completion date
    # gets stamped with today's date (a one-time fill — it only ever touches
    # rows where completed_at is still NULL).
    conn.execute(
        "UPDATE tasks SET completed_at = ? "
        "WHERE status = 'Done' AND completed_at IS NULL",
        (today_date(),),
    )


def now_iso():
    return datetime.now().isoformat(timespec="seconds")


def today_date():
    return date.today().isoformat()


# --------------------------------------------------------------------------- #
# Helpers
# --------------------------------------------------------------------------- #
def parse_due(value):
    """Accept YYYY-MM-DD (validated) or empty string to clear. Returns the
    normalized string or raises ValueError."""
    if value is None:
        return None
    value = value.strip()
    if value == "":
        return ""  # caller treats "" as 'clear the field'
    try:
        datetime.strptime(value, "%Y-%m-%d")
    except ValueError:
        raise ValueError(f"due date must be YYYY-MM-DD, got: {value!r}")
    return value


def get_task_row(conn, task_id):
    row = conn.execute("SELECT * FROM tasks WHERE id = ?", (task_id,)).fetchone()
    if row is None:
        raise ValueError(f"no task with id {task_id}")
    return row


def task_to_dict(conn, row):
    tags = [
        r["tag"]
        for r in conn.execute(
            "SELECT tag FROM tags WHERE task_id = ? ORDER BY tag", (row["id"],)
        )
    ]
    links = [
        {"id": r["id"], "url": r["url"], "label": r["label"]}
        for r in conn.execute(
            "SELECT id, url, label FROM links WHERE task_id = ? ORDER BY id",
            (row["id"],),
        )
    ]
    d = dict(row)
    d["tags"] = tags
    d["links"] = links
    d["overdue"] = is_overdue(d)
    return d


def is_overdue(task):
    if not task.get("due_date"):
        return False
    if task.get("status") in ("Done", "Cancelled"):
        return False
    try:
        return datetime.strptime(task["due_date"], "%Y-%m-%d").date() < date.today()
    except ValueError:
        return False


def add_tags(conn, task_id, tags):
    for t in tags:
        t = t.strip().lstrip("#")
        if t:
            conn.execute(
                "INSERT OR IGNORE INTO tags (task_id, tag) VALUES (?, ?)",
                (task_id, t),
            )


def add_link(conn, task_id, url, label=None):
    conn.execute(
        "INSERT INTO links (task_id, url, label) VALUES (?, ?, ?)",
        (task_id, url, label),
    )


def parse_link_arg(value):
    """'url' or 'url::label' -> (url, label_or_None)."""
    if "::" in value:
        url, label = value.split("::", 1)
        return url.strip(), label.strip() or None
    return value.strip(), None


def touch(conn, task_id):
    conn.execute(
        "UPDATE tasks SET updated_at = ? WHERE id = ?", (now_iso(), task_id)
    )


# --------------------------------------------------------------------------- #
# Output formatting
# --------------------------------------------------------------------------- #
def fmt_task_line(task):
    flag = "  ⚠ OVERDUE" if task["overdue"] else ""
    due = f" · due {task['due_date']}" if task["due_date"] else ""
    tags = " " + " ".join(f"#{t}" for t in task["tags"]) if task["tags"] else ""
    return (
        f"[{task['id']}] {task['priority']} · {task['status']} · "
        f"{task['name']}{due}{tags}{flag}"
    )


def print_task_detail(task):
    print(f"[{task['id']}] {task['name']}")
    print(f"  priority : {task['priority']}")
    print(f"  status   : {task['status']}")
    print(f"  due_date : {task['due_date'] or '—'}"
          + ("  ⚠ OVERDUE" if task["overdue"] else ""))
    print(f"  effort   : {task['effort'] or '—'}")
    print(f"  tags     : {', '.join(task['tags']) if task['tags'] else '—'}")
    if task["description"]:
        print(f"  description:\n    {task['description']}")
    if task["links"]:
        print("  links:")
        for ln in task["links"]:
            label = f" ({ln['label']})" if ln["label"] else ""
            print(f"    - [{ln['id']}] {ln['url']}{label}")
    print(f"  created  : {task['created_at']}")
    print(f"  updated  : {task['updated_at']}")
    if task.get("completed_at"):
        print(f"  completed: {task['completed_at']}")


# --------------------------------------------------------------------------- #
# Commands
# --------------------------------------------------------------------------- #
def cmd_add(conn, args):
    due = parse_due(args.due) if args.due is not None else None
    if due == "":
        due = None
    ts = now_iso()
    completed = today_date() if args.status == "Done" else None
    cur = conn.execute(
        """INSERT INTO tasks (name, description, priority, status, due_date,
                              effort, created_at, updated_at, completed_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (args.name, args.description, args.priority, args.status, due,
         args.effort, ts, ts, completed),
    )
    task_id = cur.lastrowid
    if args.tag:
        add_tags(conn, task_id, args.tag)
    for link in args.link or []:
        url, label = parse_link_arg(link)
        add_link(conn, task_id, url, label)
    conn.commit()
    task = task_to_dict(conn, get_task_row(conn, task_id))
    if args.json:
        print(json.dumps(task, indent=2))
    else:
        print("Added:")
        print(" ", fmt_task_line(task))


def _build_list_query(args):
    where, params = [], []
    if not args.all and not args.status and not args.completed_since:
        where.append(f"status IN ({','.join('?' * len(ACTIVE_STATUSES))})")
        params.extend(ACTIVE_STATUSES)
    if args.status:
        where.append(f"status IN ({','.join('?' * len(args.status))})")
        params.extend(args.status)
    if args.priority:
        where.append(f"priority IN ({','.join('?' * len(args.priority))})")
        params.extend(args.priority)
    if args.tag:
        where.append(
            "id IN (SELECT task_id FROM tags WHERE tag IN "
            f"({','.join('?' * len(args.tag))}))"
        )
        params.extend(t.lstrip("#") for t in args.tag)
    if args.due_before:
        where.append("due_date IS NOT NULL AND due_date <> '' AND due_date < ?")
        params.append(args.due_before)
    if args.completed_since:
        where.append("completed_at IS NOT NULL AND completed_at >= ?")
        params.append(args.completed_since)
    clause = ("WHERE " + " AND ".join(where)) if where else ""
    return clause, params


def cmd_list(conn, args):
    clause, params = _build_list_query(args)
    rows = conn.execute(f"SELECT * FROM tasks {clause}", params).fetchall()
    tasks = [task_to_dict(conn, r) for r in rows]

    if args.overdue:
        tasks = [t for t in tasks if t["overdue"]]

    sort_key = {
        "priority": lambda t: (PRIORITY_RANK.get(t["priority"], 99),
                               t["due_date"] or "9999-99-99"),
        "due": lambda t: (t["due_date"] or "9999-99-99",
                          PRIORITY_RANK.get(t["priority"], 99)),
        "created": lambda t: t["created_at"],
        "updated": lambda t: t["updated_at"],
        "completed": lambda t: t["completed_at"] or "",
        "status": lambda t: (STATUS_RANK.get(t["status"], 99),
                            PRIORITY_RANK.get(t["priority"], 99)),
    }[args.sort]
    tasks.sort(key=sort_key, reverse=(args.sort == "completed"))

    if args.json:
        print(json.dumps(tasks, indent=2))
        return
    if not tasks:
        print("No matching tasks.")
        return
    for t in tasks:
        print(fmt_task_line(t))
    print(f"\n{len(tasks)} task(s).")


def cmd_show(conn, args):
    task = task_to_dict(conn, get_task_row(conn, args.id))
    if args.json:
        print(json.dumps(task, indent=2))
    else:
        print_task_detail(task)


def cmd_update(conn, args):
    get_task_row(conn, args.id)  # existence check
    sets, params = [], []
    if args.name is not None:
        sets.append("name = ?"); params.append(args.name)
    if args.description is not None:
        sets.append("description = ?"); params.append(args.description)
    if args.priority is not None:
        sets.append("priority = ?"); params.append(args.priority)
    if args.status is not None:
        sets.append("status = ?"); params.append(args.status)
        # Completion timestamp follows the status: set on Done, cleared otherwise.
        sets.append("completed_at = ?")
        params.append(today_date() if args.status == "Done" else None)
    if args.due is not None:
        due = parse_due(args.due)
        sets.append("due_date = ?"); params.append(None if due == "" else due)
    if args.effort is not None:
        sets.append("effort = ?")
        params.append(None if args.effort == "" else args.effort)
    if not sets:
        print("Nothing to update — pass at least one field.", file=sys.stderr)
        sys.exit(2)
    sets.append("updated_at = ?"); params.append(now_iso())
    params.append(args.id)
    conn.execute(f"UPDATE tasks SET {', '.join(sets)} WHERE id = ?", params)
    conn.commit()
    task = task_to_dict(conn, get_task_row(conn, args.id))
    if args.json:
        print(json.dumps(task, indent=2))
    else:
        print("Updated:")
        print(" ", fmt_task_line(task))


def _set_status(conn, task_id, status, as_json):
    get_task_row(conn, task_id)
    completed = today_date() if status == "Done" else None
    conn.execute(
        "UPDATE tasks SET status = ?, completed_at = ?, updated_at = ? "
        "WHERE id = ?",
        (status, completed, now_iso(), task_id),
    )
    conn.commit()
    task = task_to_dict(conn, get_task_row(conn, task_id))
    if as_json:
        print(json.dumps(task, indent=2))
    else:
        print(f"-> {status}:", fmt_task_line(task))


def cmd_status_shortcut(status):
    def runner(conn, args):
        _set_status(conn, args.id, status, args.json)
    return runner


def cmd_tag(conn, args):
    get_task_row(conn, args.id)
    add_tags(conn, args.id, args.tags)
    touch(conn, args.id)
    conn.commit()
    print_task_detail(task_to_dict(conn, get_task_row(conn, args.id)))


def cmd_untag(conn, args):
    get_task_row(conn, args.id)
    for t in args.tags:
        conn.execute(
            "DELETE FROM tags WHERE task_id = ? AND tag = ?",
            (args.id, t.lstrip("#")),
        )
    touch(conn, args.id)
    conn.commit()
    print_task_detail(task_to_dict(conn, get_task_row(conn, args.id)))


def cmd_link(conn, args):
    get_task_row(conn, args.id)
    add_link(conn, args.id, args.url, args.label)
    touch(conn, args.id)
    conn.commit()
    print_task_detail(task_to_dict(conn, get_task_row(conn, args.id)))


def cmd_unlink(conn, args):
    cur = conn.execute("DELETE FROM links WHERE id = ?", (args.link_id,))
    if cur.rowcount == 0:
        print(f"No link with id {args.link_id}.", file=sys.stderr)
        sys.exit(1)
    conn.commit()
    print(f"Removed link {args.link_id}.")


def cmd_search(conn, args):
    like = f"%{args.text}%"
    rows = conn.execute(
        "SELECT * FROM tasks WHERE name LIKE ? OR description LIKE ? "
        "ORDER BY id",
        (like, like),
    ).fetchall()
    tasks = [task_to_dict(conn, r) for r in rows]
    if args.json:
        print(json.dumps(tasks, indent=2))
        return
    if not tasks:
        print("No matches.")
        return
    for t in tasks:
        print(fmt_task_line(t))
    print(f"\n{len(tasks)} match(es).")


def cmd_rm(conn, args):
    get_task_row(conn, args.id)
    conn.execute("DELETE FROM tasks WHERE id = ?", (args.id,))
    conn.commit()
    print(f"Deleted task {args.id} (and its tags/links).")


def cmd_stats(conn, args):
    by_status = dict(
        conn.execute(
            "SELECT status, COUNT(*) FROM tasks GROUP BY status"
        ).fetchall()
    )
    by_priority = dict(
        conn.execute(
            "SELECT priority, COUNT(*) FROM tasks GROUP BY priority"
        ).fetchall()
    )
    all_tasks = [task_to_dict(conn, r)
                 for r in conn.execute("SELECT * FROM tasks").fetchall()]
    overdue = sum(1 for t in all_tasks if t["overdue"])
    total = len(all_tasks)
    if args.json:
        print(json.dumps({
            "total": total,
            "by_status": by_status,
            "by_priority": by_priority,
            "overdue": overdue,
        }, indent=2))
        return
    print(f"Total tasks: {total}")
    print("By status:")
    for s in STATUSES:
        if by_status.get(s):
            print(f"  {s}: {by_status[s]}")
    print("By priority:")
    for p in PRIORITIES:
        if by_priority.get(p):
            print(f"  {p}: {by_priority[p]}")
    print(f"Overdue (active): {overdue}")


# --------------------------------------------------------------------------- #
# Argument parser
# --------------------------------------------------------------------------- #
def build_parser():
    p = argparse.ArgumentParser(
        prog="todo",
        description="Durable SQLite-backed TODO manager.",
    )
    p.add_argument("--db", default=None,
                   help="path to the SQLite file (default: $TODO_DB or ./todos.db)")
    sub = p.add_subparsers(dest="command", required=True)

    # add
    a = sub.add_parser("add", help="create a task")
    a.add_argument("name")
    a.add_argument("--description", "-d")
    a.add_argument("--priority", "-p", choices=PRIORITIES, default="P2")
    a.add_argument("--status", "-s", choices=STATUSES, default="To do")
    a.add_argument("--due", help="due date YYYY-MM-DD")
    a.add_argument("--effort", "-e",
                   help="effort estimate (suggested: XS/S/M/L/XL, but free text)")
    a.add_argument("--tag", "-t", action="append", help="repeatable")
    a.add_argument("--link", "-l", action="append",
                   help="URL, or URL::label; repeatable")
    a.add_argument("--json", action="store_true")
    a.set_defaults(func=cmd_add)

    # list
    ls = sub.add_parser("list", help="list tasks (active only by default)")
    ls.add_argument("--status", "-s", action="append", choices=STATUSES)
    ls.add_argument("--priority", "-p", action="append", choices=PRIORITIES)
    ls.add_argument("--tag", "-t", action="append")
    ls.add_argument("--due-before", help="show tasks due before YYYY-MM-DD")
    ls.add_argument("--completed-since",
                    help="show tasks completed on/after YYYY-MM-DD "
                         "(includes Done tasks automatically)")
    ls.add_argument("--overdue", action="store_true",
                    help="only overdue active tasks")
    ls.add_argument("--all", action="store_true",
                    help="include Done and Cancelled")
    ls.add_argument("--sort",
                    choices=["priority", "due", "created", "updated",
                             "completed", "status"],
                    default="priority")
    ls.add_argument("--json", action="store_true")
    ls.set_defaults(func=cmd_list)

    # show
    sh = sub.add_parser("show", help="show full detail for one task")
    sh.add_argument("id", type=int)
    sh.add_argument("--json", action="store_true")
    sh.set_defaults(func=cmd_show)

    # update
    up = sub.add_parser("update", help="edit fields on a task")
    up.add_argument("id", type=int)
    up.add_argument("--name")
    up.add_argument("--description", "-d")
    up.add_argument("--priority", "-p", choices=PRIORITIES)
    up.add_argument("--status", "-s", choices=STATUSES)
    up.add_argument("--due", help="YYYY-MM-DD, or empty string to clear")
    up.add_argument("--effort", "-e", help="empty string to clear")
    up.add_argument("--json", action="store_true")
    up.set_defaults(func=cmd_update)

    # status shortcuts
    for name, status in [("start", "In progress"), ("block", "Blocked"),
                         ("done", "Done"), ("cancel", "Cancelled"),
                         ("reopen", "To do")]:
        sp = sub.add_parser(name, help=f"set status to '{status}'")
        sp.add_argument("id", type=int)
        sp.add_argument("--json", action="store_true")
        sp.set_defaults(func=cmd_status_shortcut(status))

    # tags
    tg = sub.add_parser("tag", help="add tag(s) to a task")
    tg.add_argument("id", type=int)
    tg.add_argument("tags", nargs="+")
    tg.set_defaults(func=cmd_tag)

    ut = sub.add_parser("untag", help="remove tag(s) from a task")
    ut.add_argument("id", type=int)
    ut.add_argument("tags", nargs="+")
    ut.set_defaults(func=cmd_untag)

    # links
    lk = sub.add_parser("link", help="attach a URL to a task")
    lk.add_argument("id", type=int)
    lk.add_argument("url")
    lk.add_argument("--label")
    lk.set_defaults(func=cmd_link)

    ul = sub.add_parser("unlink", help="remove a link by its link id")
    ul.add_argument("link_id", type=int)
    ul.set_defaults(func=cmd_unlink)

    # search
    se = sub.add_parser("search", help="text search name + description")
    se.add_argument("text")
    se.add_argument("--json", action="store_true")
    se.set_defaults(func=cmd_search)

    # rm
    rm = sub.add_parser("rm", help="permanently delete a task")
    rm.add_argument("id", type=int)
    rm.set_defaults(func=cmd_rm)

    # stats
    st = sub.add_parser("stats", help="counts by status/priority + overdue")
    st.add_argument("--json", action="store_true")
    st.set_defaults(func=cmd_stats)

    return p


def main(argv=None):
    parser = build_parser()
    args = parser.parse_args(argv)
    db_path = args.db or default_db_path()
    conn = connect(db_path)
    try:
        args.func(conn, args)
    except ValueError as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)
    finally:
        conn.close()


if __name__ == "__main__":
    main()
