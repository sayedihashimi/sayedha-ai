# Copilot Instructions — sayedha-ai

## Repo Purpose

This repository contains reusable GitHub Copilot customizations: **agent skills**, **custom instructions**, and **research/best-practices documentation**. It is not a typical application — there is no build system, no tests, and no source code to compile. The primary artifacts are Markdown files, JSON templates, and PowerShell scripts.

## Directory Structure

| Directory | Contents |
|-----------|----------|
| `.agents/skills/<name>/` | Agent skills (each skill is a folder with a `SKILL.md` entry point) |
| `plugins/<name>/` | Distributable plugins (each plugin has a `plugin.json` manifest, hooks, scripts, etc.) |
| `instructions/` | Reusable custom instruction files (`.instructions.md`) — legacy, prefer plugins |
| `scripts/` | PowerShell helper scripts — legacy, prefer plugin-bundled scripts |
| `research/` | Background research and best-practices documentation |
| `.github/prompts/` | Prompt files for one-off or planning tasks |

## When to Use Each Customization Type

| Type | Location | Use When |
|------|----------|----------|
| **Custom instructions** | `instructions/` or `.github/copilot-instructions.md` | Always-on rules that apply to every task (coding standards, conventions, policies) |
| **Agent skills** | `.agents/skills/<name>/` | Detailed, task-specific guidance that Copilot should load only when relevant |
| **Prompt files** | `.github/prompts/` | Manual, one-off tasks triggered explicitly by the user |

## Skill Authoring Conventions

Skills in this repo go in `.agents/skills/<name>/`. Every skill must follow the conventions below.

### Folder and File Structure

```
.agents/skills/<name>/
├── SKILL.md              # Required — skill entry point
├── reference/            # Background docs, API references
├── examples/             # Golden input→output examples
├── templates/            # Starter files, boilerplate
└── scripts/              # Executable scripts referenced by SKILL.md
```

### SKILL.md Format

Every `SKILL.md` must have YAML frontmatter with `name` and `description`. Optionally include `license`.

```yaml
---
name: my-skill-name
description: "Creates [output] for [context]. Use when [trigger conditions]."
---
```

### Naming Rules

- `name` field: lowercase, hyphens only, 1–64 characters. No spaces, underscores, or uppercase.
- Folder name must exactly match the `name` field in the YAML frontmatter.

### Description Rules

- 10–1024 characters.
- State what the skill does AND when to activate it.
- Include specific technologies, frameworks, file types, or task types as trigger keywords.
- Avoid vague terms like "helps with" or "assists" without specifics.
- Pattern: `"Generates [output] for [context]. Use when [trigger conditions]."`

### Body Rules

- Write instructions as numbered steps with clear success criteria per step.
- Use imperative voice: "Create", "Run", "Verify" — not "You might consider".
- Keep the body under 1500 words. Move verbose content to `reference/`.
- Include at least one inline example showing expected input → output.
- Reference supporting files with relative paths: `See reference/api-guide.md`.
- Do not embed repo-wide policies — those belong in custom instructions.
- Do not hardcode version numbers, URLs, or other volatile data. Put those in `reference/` files.

### Supporting Files

- Place background docs in `reference/`, golden examples in `examples/`, file templates in `templates/`, executable scripts in `scripts/`.
- Every skill should have at least one golden example in `examples/` showing a complete, realistic scenario.
- Scripts must be self-contained with documented dependencies and exact run commands in SKILL.md.

### Design Principles

- One skill = one workflow. If a skill covers multiple unrelated workflows, split it.
- Prefer small, composable skills over large monolithic ones.
- Before creating a skill, verify the guidance does not belong in instructions (always-on rules) or a prompt file (manual one-off tasks).

### Quality Checks

- Ensure no placeholder text remains (TBD, TODO, FIXME, "add later").
- Test the skill with at least 3 different prompt variations to verify activation and output quality.

### Canonical Example

See `.agents/skills/dotnet-new-template/` for a well-structured skill with reference docs, examples, templates, and a validation script.

## Commit Policy

Always commit changes to this repository after making modifications. Do not leave uncommitted work.

## General Conventions

- Use Markdown for all documentation files.
- Use lowercase with hyphens for file and directory names (e.g., `my-skill-name`, `template-json-reference.md`).
- Keep lines readable; use tables for structured comparisons.
- When adding new skills, update the `README.md` to include a summary and link.
