# Plan: .NET Template Authoring Skill (`dotnet-new-template`)

## Problem

Create a GitHub Copilot agent skill that brings deep expertise in .NET project, item, and solution template authoring. The skill should make it easy for template authors to create, configure, validate, test, and package .NET templates for use with `dotnet new` and Visual Studio.

## Proposed Approach

Build a modular skill following the agentskills.io specification and Copilot skill best practices. The SKILL.md is kept lean (<1500 words) with procedural steps. Verbose reference material (template.json schema, symbols, IDE integration, packaging) lives in `reference/` files loaded on-demand. Golden examples demonstrate complete, realistic templates. A validation script checks template.json for common mistakes.

The skill is modeled after [199-biotechnologies/claude-deep-research-skill](https://github.com/199-biotechnologies/claude-deep-research-skill) — lean SKILL.md entry point with rich supporting files.

## Key Design Decisions

- **Skill location:** `skills/dotnet-new-template/` at the repo root (all skills under a `skills/` folder)
- **Skill name:** `dotnet-new-template` (matches folder name)
- **Languages:** C# only
- **Template types:** Project, item, and solution templates
- **Advanced features:** Covers symbols, computed/generated symbols, conditional content, post-actions, constraints
- **NuGet packaging:** Included
- **Validation script:** PowerShell script that lints template.json files
- **Existing project detection:** When cwd has a .NET project, assume user wants to templatize it

## Primary Resources

- https://learn.microsoft.com/en-us/dotnet/core/tutorials/cli-templates-create-project-template
- https://learn.microsoft.com/en-us/dotnet/core/tutorials/cli-templates-create-item-template
- https://github.com/sayedihashimi/template-sample
- https://github.com/dotnet/templating/wiki (especially Reference-for-template.json)
- http://json.schemastore.org/template

## Skill File Structure

```
skills/
  dotnet-new-template/
    SKILL.md                              # Main skill definition (lean, <1500 words)
    reference/
      template-json-reference.md          # template.json field reference & schema
      ide-host-json-reference.md          # Visual Studio/VS Code integration via ide.host.json
      symbols-and-parameters.md           # Parameters, computed/generated/derived symbols, conditional content
      solution-templates.md               # Multi-project/solution template specifics
      nuget-packaging.md                  # Packaging templates as NuGet packages (.nupkg)
      post-actions-and-constraints.md     # Post-actions registry and template constraints
    examples/
      project-template-example.md         # Complete console app project template
      item-template-example.md            # Complete item template (class with extension methods)
      solution-template-example.md        # Complete multi-project solution template
    templates/
      project-template.json               # Starter template.json for project templates
      item-template.json                  # Starter template.json for item templates
      solution-template.json              # Starter template.json for solution templates
      ide.host.json                       # Starter ide.host.json for Visual Studio
    scripts/
      Validate-DotnetTemplate.ps1         # Validates template.json files for common issues
```

## Todos

### 1. Create SKILL.md (the lean entry point)

The SKILL.md body should include:
- YAML frontmatter: `name: dotnet-new-template`, behavior-driven `description` with trigger keywords
- A decision tree: detect cwd project → determine template type → gather info → create files
- Numbered steps for the primary workflow:
  1. Detect existing .NET project in cwd (look for `*.csproj`, `*.sln`)
  2. Ask user for missing info: template type (project/item/solution), name, shortName, identity, classifications
  3. Create `.template.config/` directory
  4. Generate `template.json` with all required fields + sourceName + symbols
  5. Generate `ide.host.json` for Visual Studio integration
  6. Optionally configure NuGet packaging (template pack .csproj)
  7. Run the validation script
  8. Provide install and test commands
- One inline example showing minimal template.json input → output
- References to supporting files for detailed guidance

### 2. Create reference/template-json-reference.md

Comprehensive reference for all template.json fields:
- Required fields: `$schema`, `author`, `identity`, `name`, `shortName`, `sourceName`, `classifications`, `tags` (language + type)
- Recommended fields: `defaultName`, `description`, `preferNameDirectory`, `symbols`, `guids`
- Optional fields: `sources`, `forms`, `postActions`, `constraints`, `primaryOutputs`
- Field-by-field explanation with valid values
- Source: dotnet/templating wiki Reference-for-template.json

### 3. Create reference/ide-host-json-reference.md

Visual Studio integration reference:
- Schema reference (`http://json.schemastore.org/vs-2017.3.host`)
- `icon` field for template icon in VS
- `symbolInfo` array for parameter visibility in VS New Project dialog
- `unsupportedHosts` to hide templates from specific IDEs
- How ide.host.json interacts with template.json symbols
- Source: sayedihashimi/template-sample

### 4. Create reference/symbols-and-parameters.md

Deep reference on template symbols:
- Parameter symbols (bool, choice, text, int, float, hex)
- `replaces` and `fileRename` mechanics
- Generated symbols (port, guid, now, random, casing, coalesce, etc.)
- Derived symbols (valueSource + valueTransform)
- Computed symbols (boolean expressions)
- Bind symbols (host-provided values)
- Multi-choice symbols
- Value forms (transforms)
- Conditional content with source modifiers
- Source: dotnet/templating wiki

### 5. Create reference/solution-templates.md

Multi-project solution template specifics:
- `.template.config` folder placement (root of solution)
- `tags.type` set to `solution`
- Solution name best practices (avoid matching sourceName)
- `primaryOutputs` for multi-project loading
- GUID replacement for project IDs in .sln files
- `editorTreatAs` tag set to `solution`
- Rename mechanics for .sln files
- Source: sayedihashimi/template-sample

### 6. Create reference/nuget-packaging.md

Template NuGet packaging guide:
- Creating a template pack .csproj (`<PackageType>Template</PackageType>`)
- Required NuGet metadata (PackageId, Version, Description, Authors)
- Building the package (`dotnet pack`)
- Publishing to nuget.org or local feeds
- Installing from NuGet (`dotnet new install <PackageId>`)
- Versioning and updates
- Source: MS Learn tutorials

### 7. Create reference/post-actions-and-constraints.md

Post-actions and constraints reference:
- Post-action registry (restore NuGet packages, open file, run script, add reference)
- Template constraints (OS, host, SDK version)
- Conditional post-actions
- Source: dotnet/templating wiki Post-Action-Registry

### 8. Create examples/project-template-example.md

Complete, realistic project template example:
- A console app template with:
  - Proper template.json (all required + recommended fields)
  - Framework choice parameter (net8.0, net9.0)
  - Author parameter with replaces
  - sourceName configured
  - ide.host.json with symbol visibility
- Directory tree showing file layout
- Install and test commands

### 9. Create examples/item-template-example.md

Complete item template example:
- A C# class file template with:
  - ClassName parameter with replaces + fileRename
  - tags.type = "item"
- Directory tree, install, and test commands

### 10. Create examples/solution-template-example.md

Complete solution template example:
- A multi-project solution (e.g., web app + class library) with:
  - tags.type = "solution"
  - GUID replacements
  - primaryOutputs
  - Solution file rename mechanics
- Directory tree, install, and test commands

### 11. Create starter templates in templates/

Four starter JSON files that the skill uses when generating template configs:
- `project-template.json` — minimal but complete project template.json
- `item-template.json` — minimal but complete item template.json
- `solution-template.json` — minimal but complete solution template.json
- `ide.host.json` — starter ide.host.json with common symbolInfo

### 12. Create scripts/Validate-DotnetTemplate.ps1

PowerShell validation script that checks:
- Required fields present: `author`, `identity`, `name`, `shortName`, `sourceName`, `classifications`, `tags`, `tags.language`, `tags.type`
- `tags.type` is one of: `project`, `item`, `solution`
- `sourceName` matches a value found in the project content
- `shortName` follows conventions (lowercase, no spaces)
- Warnings for missing recommended fields: `defaultName`, `description`, `symbols`, `$schema`
- Checks for existence of ide.host.json
- Reports errors vs warnings
- Accepts path to template folder or template.json file
- Inspired by: sayedha.template.command analyzer

### 13. Update README.md

Update the repo README to document:
- The new skills/ directory structure
- How to use the dotnet-new-template skill
- Link to SKILL.md

## SKILL.md Workflow (Agent Behavior)

When the skill activates, the agent should follow this decision tree:

```
User Request
├── CWD has *.csproj or *.fsproj?
│   ├── YES → Offer to templatize existing project
│   │   ├── Detect project name from .csproj
│   │   ├── Use project name as sourceName
│   │   ├── Infer classifications from project type
│   │   └── Ask user for: shortName, identity, template display name
│   └── NO → Ask user what kind of template to create
│       └── Ask for: template type, project structure details
├── CWD has *.sln?
│   └── Offer to create solution template
│       ├── Detect project references
│       └── Configure primaryOutputs and GUID replacements
└── No .NET files detected
    └── Ask user for full template requirements
        └── Create template from scratch

For ALL paths:
1. Create .template.config/ directory
2. Generate template.json from appropriate starter template
3. Generate ide.host.json for Visual Studio support
4. Configure sourceName for namespace replacement
5. Add symbols/parameters based on user requirements
6. Run Validate-DotnetTemplate.ps1
7. Provide `dotnet new install` and test commands
8. Optionally set up NuGet packaging
```

## SKILL.md Description (draft)

```
Creates and configures .NET project, item, and solution templates for dotnet new and Visual Studio.
Use when creating template.json, ide.host.json, configuring template parameters/symbols,
packaging templates as NuGet packages, or converting existing .NET projects into reusable templates.
Triggers on: dotnet new template, template.json, .template.config, template authoring, item template,
project template, solution template, NuGet template package.
```

## Quality Checklist (pre-flight)

Before considering the skill complete, verify against the skill checklist:
- [ ] `name` matches folder name (`dotnet-new-template`)
- [ ] Description is behavior-driven with trigger keywords
- [ ] Description is 10-1024 characters
- [ ] SKILL.md body is under 1500 words
- [ ] At least one inline example in SKILL.md body
- [ ] At least one golden example in `examples/`
- [ ] No hardcoded version numbers in SKILL.md (put in reference/)
- [ ] References use relative paths
- [ ] Scripts are self-contained with documented dependencies
- [ ] No placeholder text (TBD, TODO, FIXME)
- [ ] Tested with 2-3 prompt variations
