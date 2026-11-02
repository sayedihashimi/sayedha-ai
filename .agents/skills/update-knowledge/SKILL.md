---
name: update-knowledge
description: Consolidate important session knowledge into project-level and feature-level Markdown files.
disable-model-invocation: true
---

# Project Knowledge Maintenance

Maintain structured, persistent knowledge across project-level and feature-level Markdown files.

## Objectives

- Preserve important discoveries across sessions.
- Maintain an accurate record of decisions and their rationale.
- Organize knowledge at the appropriate level of the project hierarchy.
- Prevent duplication and unnecessary growth.
- Make knowledge reusable by other AI agents.
- Keep automatically maintained knowledge separate from authored deliverables.

## Project Structure

The project generally follows this structure:

```text
project/
├── CLAUDE.md
├── .claude/
│   └── skills/
│       └── update-knowledge/
│           └── SKILL.md
├── memory/
│   ├── decisions.md
│   ├── project-status.md
│   ├── research.md
│   └── open-questions.md
├── strategy/          # authored deliverables only, no knowledge files
└── features/
    └── <feature-name>/
        ├── research.md
        ├── decisions.md
        ├── open-questions.md
        └── status.md
```

Feature directories and knowledge files may be created as needed.

Do not create empty files or directories unnecessarily.

Knowledge files live only in `memory/` and `features/<feature-name>/`. Never write knowledge files into `strategy/` or any other workstream directory. Those hold authored deliverables. If you find knowledge files in a workstream directory, don't add to them. Report them and offer to merge them into `memory/`.

## Knowledge Classification

Classify each meaningful discovery before saving it.

### Product-Level Knowledge

Store information in the root `memory/` directory when it:

- Affects overall product strategy.
- Establishes product-wide priorities.
- Changes positioning or target audiences.
- Impacts multiple features.
- Represents a product-wide decision.
- Changes the overall project status.

### Feature-Level Knowledge

Store information in `features/<feature-name>/` when it:

- Concerns a specific feature or initiative.
- Describes feature requirements.
- Documents feature-specific research.
- Records technical or implementation considerations.
- Captures feature-specific decisions.
- Identifies unresolved feature questions.
- Changes the status of a feature.

### Cross-Feature Knowledge

When information affects multiple features:

1. Store detailed findings in the most relevant feature directory.
2. Add a concise summary to the appropriate root memory file.
3. Link to the detailed source.
4. Avoid duplicating full content.
5. Preserve relationships between related features.

If there is no clear primary feature, store the shared finding at the project level and link to it from relevant feature files when useful.

## Active Feature Identification

Determine the active feature using the following priority:

1. The active feature explicitly established by `/work-on-feature` in the current conversation.
2. The user's most recent explicit instructions.
3. Files being discussed or modified.
4. The current working directory.

If `/work-on-feature` established an active feature, use that feature's directory for feature-specific knowledge unless the user explicitly changes the active feature.

Only directories under `features/` are features. If the active context is a workstream outside `features/`, such as `strategy/`, store its knowledge in the root `memory/` directory.

Do not infer a different active feature merely because another feature is mentioned in passing.

The active feature is a logical context, not a filesystem restriction.

If the feature is unclear:

- Do not guess.
- Do not classify feature-specific information as product-wide merely because its destination is unknown.
- Ask for clarification before writing ambiguous information.

## Knowledge Extraction

Review the current conversation for meaningful information worth preserving.

### Capture

- Confirmed facts
- Important decisions
- Decision rationale
- Research findings
- Supporting evidence and source links
- Changes in priorities
- Changes in project or feature status
- Completed work
- Unresolved questions
- Next steps
- Important constraints or assumptions

### Do Not Capture

- Casual conversation
- Temporary or incidental details
- Repeated information
- Unverified claims presented as facts
- Speculation without clear qualification
- Secrets, credentials, or sensitive information
- Full conversation transcripts
- Intermediate reasoning that has no lasting value

Preserve useful hypotheses and unverified findings only when clearly labeled.

Record feature-level progress, status changes, completed work, and next steps in the feature's `status.md`.

## Knowledge Update Rules

Before modifying a knowledge file:

1. Read its existing contents.
2. Determine whether the new information is already represented.
3. Update or merge existing entries rather than appending duplicates.
4. Preserve important historical decisions and their rationale.
5. Mark superseded decisions rather than silently deleting them.
6. Include dates when useful.
7. Preserve supporting evidence and source links.
8. Distinguish facts, hypotheses, recommendations, and decisions.
9. Keep entries concise and easy to retrieve.
10. Preserve meaningful relationships between related findings.

If nothing meaningful has changed, make no edits.

Do not rewrite entire files when a focused update is sufficient.

## Knowledge File Responsibilities

### Root `memory/decisions.md`

Contains product-wide decisions.

Each significant decision should include, when available:

- Decision
- Date
- Rationale
- Alternatives considered
- Implications
- Status: Active, Superseded, or Reversed
- Related feature or research links

### Root `memory/project-status.md`

Contains a concise overview of:

- Current product objectives
- Active initiatives
- Important milestones
- Current priorities
- Major blockers
- Significant recent changes

List active features in a line each, linking to each feature's `status.md`, instead of repeating their detail.

Avoid duplicating detailed feature status.

### Root `memory/research.md`

Contains reusable product-wide findings:

- Market research
- Competitive analysis
- Customer insights
- Cross-feature discoveries
- Supporting sources

Prefer linking to detailed research documents rather than copying them.

### Root `memory/open-questions.md`

Contains unresolved product-wide questions.

When a question is resolved:

- Record the resolution where appropriate.
- Remove it from the active open-question list.
- Preserve significant decisions in `decisions.md`.

### Feature `research.md`

Contains reusable research specific to the active feature.

Include:

- Findings
- Supporting evidence
- Source links
- Relevant constraints
- Competing approaches
- Areas requiring further validation

### Feature `decisions.md`

Contains feature-specific decisions.

Include:

- Decision
- Date
- Rationale
- Alternatives considered
- Implications
- Status
- Supporting evidence

### Feature `open-questions.md`

Contains unresolved questions specific to the feature.

Include:

- Question
- Context
- Why it matters
- Potential next steps

Remove resolved questions from the active list and preserve important resolutions elsewhere.

### Feature `status.md`

Contains the current status of the feature.

Include:

- Current phase (e.g. idea, exploring, prototyping, shipped, parked)
- Priority or position relative to other features, when one was set
- Work completed, with dates
- Next steps
- Blockers
- Links to the feature's research, decisions, and open questions

Create it only when there is real status to record.

The feature status file is always named `status.md`. `project-status.md` is the root-level file in `memory/` only. If a feature directory has a `project-status.md`, treat it as the feature's `status.md`: update it in place and offer to rename it. Don't create a second status file next to it.

If a feature has no status file yet, and its status has so far been kept in its `README.md` or in `memory/project-status.md`, create `status.md` when there is new status to record. Leave the existing status text in the README and in root memory where it is. Offer to cut the feature's detail in `memory/project-status.md` down to one line that links to the new `status.md`.

## File Creation

Create knowledge files only when meaningful information needs to be preserved.

Rules:

- Prefer existing files over creating new ones.
- Do not create empty files.
- Do not create duplicate knowledge documents.
- Use descriptive filenames.
- Follow the project's existing naming conventions.
- Do not reorganize existing directories without permission.

When a feature directory does not exist, ask before creating it.

## Strategy Document Protection

Never automatically modify authored deliverables, including:

- Strategy documents
- Product proposals
- Roadmaps
- Requirements documents
- Presentations
- Customer-facing documents

These documents may be read for context.

Only modify them when the user explicitly requests changes.

Automatically maintained knowledge files must remain separate from authored deliverables.

## Session Knowledge Maintenance

When invoked, review the current conversation and identify knowledge that should persist beyond the session.

Prioritize:

1. Decisions that would be difficult to reconstruct.
2. Research findings that required significant effort.
3. Important changes to project or feature direction.
4. Unresolved questions that affect future work.
5. Progress that future sessions need to understand. Feature-level progress goes in the feature's `status.md`.

Avoid recording every conversational detail.

If the current session covers multiple features, classify findings individually rather than assigning everything to the active feature.

## Consistency Checks

After updating knowledge:

1. Verify that each finding is stored at the correct level.
2. Check for duplication.
3. Check for contradictions with existing knowledge.
4. Preserve unresolved contradictions explicitly.
5. Verify relative Markdown links.
6. Ensure decisions are not accidentally represented as established facts.
7. Keep summaries concise.

Do not silently resolve conflicting information.

## Completion Behavior

### Manual Invocation

When invoked using `/update-knowledge`:

- Update the appropriate knowledge files.
- Briefly summarize which files changed.
- Explain significant decisions or conflicts discovered.
- Report when no updates were necessary.

### Automatic Maintenance

When performing routine knowledge maintenance as part of normal work:

- Update only when meaningful information changes.
- Avoid unnecessary file modifications.
- Do not interrupt the conversation with routine status messages.
- Do not ask for confirmation unless information is ambiguous, sensitive, or potentially destructive.

## Safety

- Never store secrets, credentials, access tokens, or private keys.
- Avoid copying sensitive personal information into knowledge files.
- Preserve the distinction between internal and public information.
- Do not publish or transmit project knowledge externally.
- Do not delete historical decisions without explicit permission.
- Do not modify files outside the project unless explicitly authorized
