---
name: dotnet-new-template
description: "Creates and configures .NET project, item, and solution templates for dotnet new and Visual Studio. Use when creating template.json, ide.host.json, configuring template parameters and symbols, packaging templates as NuGet packages, or converting existing .NET projects into reusable templates. Triggers on: dotnet new template, template.json, .template.config, template authoring, item template, project template, solution template, NuGet template package."
---

# .NET Template Authoring

## Core Purpose

Guide the creation of high-quality .NET templates for `dotnet new` and Visual Studio. Detect existing projects, gather requirements, generate all configuration files, validate, and provide install/test commands.

---

## Decision Tree

```
User Request
├── CWD has *.csproj? → Templatize existing project
│   ├── Extract project name → use as sourceName
│   ├── Detect project SDK → infer classifications
│   └── Ask: shortName, identity, display name
├── CWD has *.sln? → Create solution template
│   ├── Detect project references → configure primaryOutputs
│   └── Extract GUIDs → add to guids array
└── No .NET files → Create from scratch
    └── Ask: template type (project/item/solution), structure
```

---

## Workflow

1. **Detect context.** Search CWD for `*.csproj`, `*.fsproj`, `*.sln` files. If found, offer to convert the existing project/solution into a template.

2. **Gather requirements.** Ask the user for any missing information:
   - Template type: `project`, `item`, or `solution`
   - `name` (display name for `dotnet new list`)
   - `shortName` (CLI short name, lowercase, no spaces)
   - `identity` (unique identifier, e.g. `CompanyName.ProjectType.CSharp`)
   - `author`
   - `classifications` (tags: Console, Web, Library, etc.)
   - `sourceName` (text to replace with user's project name)
   - Target frameworks (if applicable)
   - Custom parameters/symbols needed

3. **Create `.template.config/` directory** at the template root.

4. **Generate `template.json`.** Use the appropriate starter template from `templates/` and populate all fields. See `reference/template-json-reference.md` for the full field reference. Always include:
   - `$schema`: `http://json.schemastore.org/template`
   - All required fields: `identity`, `name`, `shortName`, `sourceName`, `author`, `classifications`, `tags`
   - `tags.language`: `C#` and `tags.type`: `project`|`item`|`solution`
   - `defaultName` for Visual Studio
   - `preferNameDirectory: true` for project templates

5. **Add symbols/parameters** based on requirements. See `reference/symbols-and-parameters.md` for parameter types, generated symbols, conditional content, and value forms.

6. **Generate `ide.host.json`** for Visual Studio integration. See `reference/ide-host-json-reference.md`. Include `symbolInfo` entries for each user-visible parameter.

7. **Configure NuGet packaging** (if requested). See `reference/nuget-packaging.md`. Create a template pack `.csproj` with `<PackageType>Template</PackageType>`.

8. **Validate the template** by running:
   ```
   pwsh -File scripts/Validate-DotnetTemplate.ps1 -TemplatePath <path-to-template-folder>
   ```
   Fix any errors reported. See `reference/post-actions-and-constraints.md` for post-actions and constraints configuration.

9. **Provide install and test commands:**
   ```
   dotnet new install .\                          # Install from folder
   dotnet new <shortName> -n MyTestProject        # Create test project
   dotnet build                                   # Verify it builds
   dotnet new uninstall .\                        # Uninstall when done
   ```

---

## Inline Example

**Input:** User has a console app project `MyTool` in CWD with `MyTool.csproj`.

**Output:** Generated `.template.config/template.json`:

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Contoso",
  "classifications": ["Console"],
  "identity": "Contoso.MyTool.CSharp",
  "name": "Contoso Console Tool",
  "shortName": "mytool",
  "sourceName": "MyTool",
  "defaultName": "MyTool",
  "preferNameDirectory": true,
  "tags": {
    "language": "C#",
    "type": "project"
  },
  "symbols": {
    "Framework": {
      "type": "parameter",
      "description": "The target framework for the project.",
      "datatype": "choice",
      "choices": [
        { "choice": "net8.0", "description": "Target .NET 8.0" },
        { "choice": "net9.0", "description": "Target .NET 9.0" }
      ],
      "replaces": "net8.0",
      "defaultValue": "net8.0"
    }
  }
}
```

---

## Reference Files

| File | Use When |
|------|----------|
| `reference/template-json-reference.md` | Looking up any template.json field |
| `reference/ide-host-json-reference.md` | Configuring Visual Studio integration |
| `reference/symbols-and-parameters.md` | Adding parameters, computed/generated symbols, conditional content |
| `reference/solution-templates.md` | Creating multi-project solution templates |
| `reference/nuget-packaging.md` | Packaging templates for NuGet distribution |
| `reference/post-actions-and-constraints.md` | Adding post-create actions or platform constraints |

## Starter Templates

Use the JSON files in `templates/` as starting points when generating template configuration:
- `templates/project-template.json` — project template starter
- `templates/item-template.json` — item template starter
- `templates/solution-template.json` — solution template starter
- `templates/ide.host.json` — Visual Studio host file starter

## Golden Examples

See `examples/` for complete, realistic template configurations:
- `examples/project-template-example.md` — console app with Framework choice
- `examples/item-template-example.md` — class file with ClassName parameter
- `examples/solution-template-example.md` — multi-project solution template
