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

Always consult project-level knowledge when available:

- `CLAUDE.md`
- `memory/project-status.md`
- `memory/decisions.md`
- `memory/research.md`
- `memory/open-questions.md`

For a feature, also read these files from the feature directory:

- `README.md`
- `research.md`
- `decisions.md`
- `open-questions.md`
- `status.md`

If the feature directory has a `project-status.md` instead of `status.md`, read it as the feature's status file and report it in the completion summary as a legacy name.

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
- Use the standard filenames. In `memory/`: `research.md`, `decisions.md`, `open-questions.md`, `project-status.md`. In a feature directory: `research.md`, `decisions.md`, `open-questions.md`, `status.md`. Don't invent others, such as `context.md`.
- If `memory/` doesn't exist, create it when the first knowledge file is written. Don't create it empty.
- Avoid duplicating knowledge across levels.
- Preserve decisions and supporting evidence.
- Do not modify authored strategy documents unless explicitly requested.
- If the destination is unclear, ask before writing.

Do not automatically invoke unrelated skills.

### 6. Maintain focus

For subsequent tasks:

- Prefer the active feature's documents as the primary context.
- Consider product-level strategy and decisions when relevant.
- Do not assume all requests relate to the active feature.
- Ask for clarification if a request appears to involve another feature.

### 7. Completion

Confirm:

- Project name
- Active feature
- Feature directory, and whether it is a feature or a workstream
- Knowledge directory, where new knowledge will be saved
- Existing knowledge files discovered
- Misplaced knowledge files, if any

Then wait for the user's next task.

Do not begin researching or modifying feature content until requested.
