---
name: work-on-feature
description: Set the active feature or workstream for the current Claude Code session.
disable-model-invocation: true
argument-hint: <feature-directory>
---

# Work on Feature

Establish a feature or workstream as the active context for the current conversation.

## Arguments

The user provides the feature directory name or relative path:

$ARGUMENTS

## Instructions

### 1. Identify the project

- Use the current working directory as the project root.
- Do not change the working directory.
- Do not create a new Claude Code project.
- Do not modify Claude Code's configuration.

### 2. Resolve the feature directory

Resolve the supplied argument using the following order:

1. An existing directory relative to the project root.
2. An existing directory under `features/`.
3. An existing directory under `workstreams/`.

If multiple directories match, ask the user to choose.

If no matching directory exists, explain the problem and ask whether the user wants to create it.

Do not silently create directories.

Then classify the resolved directory:

- **Feature**: a directory under `features/`.
- **Workstream**: any other directory, such as `strategy/`. A workstream directory holds authored deliverables (strategy docs, proposals, planning summaries), not knowledge files.

### 3. Establish active feature context

Treat the resolved directory as the active feature for the remainder of this conversation, unless the user explicitly changes it.

All subsequent requests should be interpreted in the context of this feature when appropriate.

The active feature is a logical context, not a filesystem restriction.

### 4. Load existing knowledge

Load knowledge in two tiers, and make all the reads in a single message of parallel tool calls.

**Read in full:**

- `CLAUDE.md`
- `memory/project-status.md`
- For a feature: the feature's `status.md`, `open-questions.md` and `README.md`

**Read only the frontmatter, the `## Summary` block and the headings:**

- `memory/decisions.md`, `memory/research.md`, `memory/open-questions.md`
- For a feature: the feature's `decisions.md` and `research.md`

To do this, read the first 30 lines of each file and grep it for headings (`^#`) in the same batch. If a file has no `## Summary` block yet, read the whole file instead and note it for the housekeeping line.

Pull other sections, and anything in `archive/`, only when a task needs them.

If the feature directory has a `project-status.md` instead of `status.md`, read it as the feature's status file and report it as a legacy name.

For a workstream, list the deliverables in the workstream directory and read the ones relevant to the user's request. If the workstream directory contains knowledge files (`research.md`, `decisions.md`, `open-questions.md`, `status.md`, `project-status.md`, `context.md`), they were saved there by mistake. Read them, and report them as misplaced in the completion summary.

Only read files that exist.

Do not read unrelated feature directories unless necessary.

### 5. Knowledge management

Before writing any knowledge file, read the `update-knowledge` skill at `../update-knowledge/SKILL.md`, relative to this skill's base directory, and follow its classification and maintenance rules. That skill can't be invoked automatically, so read the file directly.

Save knowledge here:

| Active context | Feature-specific knowledge | Project-wide knowledge |
|---|---|---|
| Feature (`features/<name>/`) | `features/<name>/` | `memory/` |
| Workstream (for example `strategy/`) | `memory/` | `memory/` |

Rules:

- Never write knowledge files into a workstream directory. Workstream directories hold authored deliverables only.
- Use the standard filenames. In `memory/`: `research.md`, `decisions.md`, `open-questions.md`, `project-status.md`. In a feature directory: `research.md`, `decisions.md`, `open-questions.md`, `status.md`. Don't invent others, such as `context.md`. Older knowledge goes to the project's `archive/`, as described in the `update-knowledge` skill.
- Save knowledge as you go, when a decision is made or a finding is confirmed. Use the Edit and Write tools only, never shell or Python edits.
- Never save project knowledge in Claude Code's built-in memory folder (`~/.claude/projects/*/memory/`).
- If `memory/` doesn't exist, create it when the first knowledge file is written. Don't create it empty.
- Avoid duplicating knowledge across levels.
- Preserve decisions and supporting evidence.
- Do not modify authored strategy documents unless explicitly requested. Drafts marked `author: claude` are the exception; see the `update-knowledge` skill.
- If the destination is unclear, ask before writing.

Do not automatically invoke unrelated skills.

### 6. Maintain focus

For subsequent tasks:

- Prefer the active feature's documents as the primary context.
- Consider product-level strategy and decisions when relevant.
- Do not assume all requests relate to the active feature.
- Ask for clarification if a request appears to involve another feature.

### 7. Completion

Reply in 10 lines or fewer:

1. **Where we left off:** from the status file's `## Where we left off` block, or its latest entry and next steps if there's no block yet.
2. **Context:** project name, active feature, its directory and whether it's a feature or a workstream, and where new knowledge will be saved.
3. **Housekeeping**, only if there's something to report, in one line: misplaced knowledge files, legacy names, files without a `## Summary` block, and knowledge files over about 15 KB. Offer to tidy them up.

Don't list every file you read.

Then wait for the user's next task.

Do not begin researching or modifying feature content until requested.
