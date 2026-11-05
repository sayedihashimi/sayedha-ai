#!/usr/bin/env python3
"""Build a compact Markdown digest of recent Claude Code sessions.

Reads the transcript files under ~/.claude/projects/<project>/<session>.jsonl and,
for every session with user activity in the window, prints its working folder,
title, time span, the user's prompts and the last assistant reply. Short
throwaway sessions (one prompt) are grouped per folder so they don't drown out
real work.

Usage:
  python -I extract_sessions.py [--days 7] [--since YYYY-MM-DD] [--until YYYY-MM-DD]
                                [--projects-dir DIR] [--max-prompts 10]
                                [--prompt-chars 300] [--exclude-session ID ...]
                                [--output FILE]

The digest only contains text the user typed and short assistant excerpts; tool
output is skipped. Likely secrets (JWTs, bearer tokens, known API-key formats,
key=value secrets, private keys, long random strings) are replaced with
[redacted] before anything is written. Redaction is pattern-based, so it can
miss unusual formats; still review the digest before sharing it.
"""
import argparse
import glob
import json
import os
import re
import sys
from collections import defaultdict
from datetime import datetime, timedelta, timezone

NOISE_PREFIXES = (
    "<local-command",
    "<command-stdout",
    "<command-stderr",
    "<bash-stdout",
    "<bash-stderr",
    "<task-notification",
    "Caveat:",
    "[Request interrupted",
)


REDACTED = "[redacted]"
SECRET_PATTERNS = [
    # PEM private keys
    re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----.*?(-----END [A-Z ]*PRIVATE KEY-----|$)", re.S),
    # JWTs (also catches a JWT cut off before its first dot)
    re.compile(r"\beyJ[A-Za-z0-9_-]{10,}(\.[A-Za-z0-9_-]+){0,2}"),
    # Authorization headers
    re.compile(r"(?i)\b(bearer|basic)\s+[A-Za-z0-9._~+/=-]{12,}"),
    # Well-known key prefixes
    re.compile(r"\b(sk|rk|pk)-[A-Za-z0-9_-]{16,}"),
    re.compile(r"\b(ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{20,}"),
    re.compile(r"\bgithub_pat_[A-Za-z0-9_]{20,}"),
    re.compile(r"\bglpat-[A-Za-z0-9_-]{16,}"),
    re.compile(r"\bxox[abprse]-[A-Za-z0-9-]{10,}"),
    re.compile(r"\b(AKIA|ASIA)[0-9A-Z]{16}\b"),
    re.compile(r"\bAIza[0-9A-Za-z_-]{30,}"),
    re.compile(r"\bya29\.[0-9A-Za-z_-]{20,}"),
    re.compile(r"\bnpm_[A-Za-z0-9]{30,}"),
]
# key=value / key: value where the key name says it's a secret; keep the key, hide the value
SECRET_ASSIGN = re.compile(
    r"(?i)\b([\w-]*(?:api[_-]?key|access[_-]?key|secret|token|passw(?:or)?d|pwd|credential)s?)"
    r"(\s*[:=]\s*[\"']?)([^\s\"',;&]{6,})"
)
# Long random-looking strings: 32+ chars of a base64/url alphabet with upper, lower and digits
LONG_RANDOM = re.compile(r"(?<![\w/\\.-])[A-Za-z0-9+/_=-]{32,}(?![\w/\\.-])")


def looks_random(s):
    return (
        re.search(r"[a-z]", s) and re.search(r"[A-Z]", s) and re.search(r"\d", s)
        and not re.fullmatch(r"[0-9a-fA-F-]+", s)  # hashes / GUIDs
    )


def redact(text):
    """Mask likely secrets. Runs before truncation so cut-off tokens are still caught."""
    for p in SECRET_PATTERNS:
        text = p.sub(REDACTED, text)
    text = SECRET_ASSIGN.sub(lambda m: m.group(1) + m.group(2) + REDACTED, text)
    text = LONG_RANDOM.sub(lambda m: REDACTED if looks_random(m.group(0)) else m.group(0), text)
    return text


def parse_args():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--days", type=float, default=7, help="look back this many days (default 7)")
    p.add_argument("--since", help="start date YYYY-MM-DD, local time (overrides --days)")
    p.add_argument("--until", help="end date YYYY-MM-DD, local time, exclusive (default now)")
    p.add_argument("--projects-dir", default=os.path.expanduser("~/.claude/projects"))
    p.add_argument("--max-prompts", type=int, default=10, help="prompts shown per session (first N-1 plus the last)")
    p.add_argument("--prompt-chars", type=int, default=300)
    p.add_argument("--exclude-session", nargs="*", default=[], help="session IDs (or prefixes) to skip")
    p.add_argument("--output", help="write the digest here instead of stdout")
    return p.parse_args()


def local_date(s):
    return datetime.strptime(s, "%Y-%m-%d").astimezone()


def text_of(content):
    if isinstance(content, str):
        return content
    if isinstance(content, list):
        return " ".join(
            x.get("text", "") for x in content if isinstance(x, dict) and x.get("type") == "text"
        )
    return ""


def clean_prompt(c):
    c = re.sub(r"<system-reminder>.*?</system-reminder>", "", c, flags=re.S)
    m = re.search(r"<command-name>(.*?)</command-name>", c, re.S)
    if m:
        args = re.search(r"<command-args>(.*?)</command-args>", c, re.S)
        c = f"{m.group(1).strip()} {args.group(1).strip() if args else ''}".strip()
    c = re.sub(r"</?pasted_content[^>]*>", " [pasted] ", c)
    c = re.sub(r"<bash-input>(.*?)</bash-input>", r"! \1", c, flags=re.S)
    return redact(re.sub(r"\s+", " ", c).strip())


def parse_ts(s):
    try:
        return datetime.fromisoformat(s.replace("Z", "+00:00"))
    except Exception:
        return None


def read_session(path, start, end):
    title = None
    cwd = None
    prompts = []
    stamps = []
    last_reply = ""
    with open(path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            try:
                d = json.loads(line)
            except Exception:
                continue
            t = d.get("type")
            if t == "custom-title" and d.get("customTitle"):
                title = d["customTitle"]
            elif t in ("ai-title", "summary") and not title:
                title = d.get("aiTitle") or d.get("summary") or title
            if d.get("isSidechain"):
                continue
            ts = parse_ts(d.get("timestamp", ""))
            if ts is None or ts < start or ts >= end:
                continue
            if t == "assistant":
                txt = text_of(d.get("message", {}).get("content")).strip()
                if txt:
                    last_reply = txt
                continue
            if t != "user" or d.get("isMeta"):
                continue
            raw = text_of(d.get("message", {}).get("content"))
            if not raw.strip() or raw.lstrip().startswith(NOISE_PREFIXES):
                continue
            if "This session is being continued from a previous conversation" in raw:
                continue
            c = clean_prompt(raw)
            if not c:
                continue
            cwd = cwd or d.get("cwd")
            stamps.append(ts)
            prompts.append(c)
    if not prompts:
        return None
    return {
        "id": os.path.basename(path)[:-6],
        "cwd": cwd or os.path.basename(os.path.dirname(path)),
        "title": redact(title) if title else title,
        "start": min(stamps).astimezone(),
        "end": max(stamps).astimezone(),
        "prompts": prompts,
        "last_reply": redact(re.sub(r"\s+", " ", last_reply)),
    }


def short(s, n):
    return s if len(s) <= n else s[: n - 1] + "…"


def main():
    a = parse_args()
    end = local_date(a.until) if a.until else datetime.now().astimezone()
    start = local_date(a.since) if a.since else end - timedelta(days=a.days)
    cutoff = start.timestamp() - 86400

    sessions = []
    for f in glob.glob(os.path.join(a.projects_dir, "*", "*.jsonl")):
        sid = os.path.basename(f)
        if any(sid.startswith(x) for x in a.exclude_session):
            continue
        if os.path.getmtime(f) < cutoff:
            continue
        try:
            s = read_session(f, start.astimezone(timezone.utc), end.astimezone(timezone.utc))
        except Exception as e:
            print(f"warning: could not read {f}: {e}", file=sys.stderr)
            continue
        if s:
            sessions.append(s)
    sessions.sort(key=lambda s: s["start"])

    real = [s for s in sessions if len(s["prompts"]) > 1]
    quick = [s for s in sessions if len(s["prompts"]) == 1]

    out = []
    w = out.append
    w(f"# Claude Code sessions {start:%Y-%m-%d %H:%M} to {end:%Y-%m-%d %H:%M} (local time)\n")
    w(f"{len(sessions)} sessions with user prompts: {len(real)} multi-prompt, {len(quick)} single-prompt.\n")

    w("## Sessions per folder\n")
    w("| Folder | Multi-prompt | Single-prompt | Prompts |")
    w("|---|---|---|---|")
    per = defaultdict(lambda: [0, 0, 0])
    for s in sessions:
        k = per[s["cwd"]]
        k[0 if len(s["prompts"]) > 1 else 1] += 1
        k[2] += len(s["prompts"])
    for cwd, (m, q, n) in sorted(per.items(), key=lambda kv: -(kv[1][0] * 3 + kv[1][1])):
        w(f"| `{cwd}` | {m} | {q} | {n} |")
    w("")

    w("## Multi-prompt sessions\n")
    for s in real:
        span = f"{s['start']:%a %m-%d %H:%M} → {s['end']:%m-%d %H:%M}"
        w(f"### {s['title'] or '(untitled)'} · {span} · {len(s['prompts'])} prompts")
        w(f"`{s['cwd']}` · session `{s['id'][:8]}`\n")
        ps = s["prompts"]
        shown = ps if len(ps) <= a.max_prompts else ps[: a.max_prompts - 1]
        for p in shown:
            w(f"- {short(p, a.prompt_chars)}")
        if len(ps) > a.max_prompts:
            w(f"- … {len(ps) - len(shown) - 1} more …")
            w(f"- (last) {short(ps[-1], a.prompt_chars)}")
        if s["last_reply"]:
            w(f"\n> Last reply: {short(s['last_reply'], 400)}")
        w("")

    w("## Single-prompt sessions, grouped by folder\n")
    groups = defaultdict(list)
    for s in quick:
        groups[s["cwd"]].append(s)
    for cwd, ss in sorted(groups.items(), key=lambda kv: -len(kv[1])):
        w(f"### `{cwd}` · {len(ss)} sessions · {ss[0]['start']:%m-%d} → {ss[-1]['start']:%m-%d}")
        seen = defaultdict(int)
        for s in ss:
            seen[short(s["prompts"][0], a.prompt_chars)] += 1
        for p, n in seen.items():
            w(f"- {p}" + (f" (×{n})" if n > 1 else ""))
        w("")

    text = "\n".join(out)
    if a.output:
        with open(a.output, "w", encoding="utf-8") as fh:
            fh.write(text)
        print(f"wrote {a.output} ({len(sessions)} sessions)")
    else:
        sys.stdout.reconfigure(encoding="utf-8")
        print(text)


if __name__ == "__main__":
    main()
