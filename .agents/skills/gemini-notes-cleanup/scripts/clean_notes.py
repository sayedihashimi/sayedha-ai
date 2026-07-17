#!/usr/bin/env python3
"""
clean_notes.py - Clean up Gemini / Google Docs meeting notes pasted as Markdown
so they are ready to drop into Obsidian.

Usage:
    python clean_notes.py input.md            # print cleaned text to stdout
    python clean_notes.py input.md output.md  # write cleaned text to output.md
    cat input.md | python clean_notes.py      # read from stdin

The script only fixes FORMATTING. It never rewords, summarizes, or reorders the
note content. Real content screenshots ( ![](data:image/...) ) are preserved
verbatim; only Gemini's checkbox images and boilerplate are removed.
"""
import re
import sys

# Gemini boilerplate / feedback prompt lines (match anywhere in the line).
BOILERPLATE_PATTERNS = [
    re.compile(r"We've updated the .+ using your feedback", re.I),
    re.compile(r"Let us know what you think:.*Helpful", re.I),
    re.compile(r"Did the screenshots in this section make your notes", re.I),
    re.compile(r"google\.qualtrics\.com", re.I),
]

# A checkbox rendered as an image, optionally as a bullet:
#   ![unchecked](data:...)   or   - ![checked](data:...)
CHECKBOX_IMG = re.compile(r"^\s*(?:[-*]\s+)?!\[(?:unchecked|checked)\]\(data:image/[^)]*\)\s*$", re.I)

# A standalone bold-wrapper line: just ** (Gemini wraps the whole note in bold).
BOLD_WRAPPER = re.compile(r"^\s*\*\*\s*$")

# A heading line.
HEADING = re.compile(r"^\s*(#{1,6})\s+(.*\S)\s*$")

# A next-step task line Gemini indents under a checkbox image,
#   "    [Sayed Hashimi] Create product brief: ..."
INDENTED_TASK = re.compile(r"^\s+(\[[^\]]+\].*\S)\s*$")


def is_blank(line: str) -> bool:
    return line.strip() == ""


def is_heading(line: str) -> bool:
    return re.match(r"^#{1,6}\s+\S", line) is not None


def transform_line(raw: str):
    """Return the cleaned line, or None if the line should be dropped."""
    if BOLD_WRAPPER.match(raw):
        return None
    if any(p.search(raw) for p in BOILERPLATE_PATTERNS):
        return None
    if CHECKBOX_IMG.match(raw):
        return None

    m = HEADING.match(raw)
    if m:
        return f"{m.group(1)} {m.group(2)}"

    m = INDENTED_TASK.match(raw)
    if m:
        return f"- {m.group(1)}"

    stripped = raw.rstrip()
    # Preserve Markdown hard breaks (content line ending in >=2 spaces).
    if stripped and (len(raw) - len(stripped)) >= 2:
        return stripped + "  "
    return stripped


def clean(text: str) -> str:
    text = text.replace("\r\n", "\n").replace("\r", "\n").lstrip("﻿")

    # Pass 1: per-line transforms (drop Nones).
    lines = [ln for ln in (transform_line(l) for l in text.split("\n")) if ln is not None]

    # Pass 2: mark which lines belong to the Summary block. The Summary keeps
    # its Gemini soft-break paragraph structure; everywhere else we tighten.
    in_summary = [False] * len(lines)
    active = False
    for i, ln in enumerate(lines):
        if is_heading(ln):
            active = ln.lower().startswith("### summary")
        in_summary[i] = active and not is_heading(ln)

    # Pass 3: rebuild with blank-line policy.
    out = []
    for i, ln in enumerate(lines):
        blank = is_blank(ln)

        if in_summary[i]:
            # Inside Summary: keep blanks but collapse consecutive ones.
            if blank and (not out or is_blank(out[-1])):
                continue
            out.append("" if blank else ln)
            continue

        if blank:
            # Outside Summary: drop blanks; spacing is re-derived below.
            continue

        # No blank line before a heading; exactly one blank line after it.
        if not is_heading(ln) and out and is_heading(out[-1]):
            out.append("")
        out.append(ln)

    # A blank line never immediately precedes a heading.
    out = [ln for i, ln in enumerate(out)
           if not (is_blank(ln) and i + 1 < len(out) and is_heading(out[i + 1]))]

    # Trim leading/trailing blank lines.
    while out and is_blank(out[0]):
        out.pop(0)
    while out and is_blank(out[-1]):
        out.pop()

    return "\n".join(out) + "\n"


def main():
    args = sys.argv[1:]
    text = open(args[0], encoding="utf-8", errors="replace").read() if args else sys.stdin.read()
    result = clean(text)
    if len(args) >= 2:
        open(args[1], "w", encoding="utf-8").write(result)
    else:
        sys.stdout.write(result)


if __name__ == "__main__":
    main()
