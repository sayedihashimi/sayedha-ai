# template.json Field Reference

Complete reference for the `.template.config/template.json` configuration file.

**Schema:** `http://json.schemastore.org/template`

---

## Required Fields

| Field | Type | Description |
|-------|------|-------------|
| `identity` | string | Unique identifier for this template (e.g., `CompanyName.ProjectType.CSharp`). Must be globally unique across all installed templates. |
| `name` | string | Display name shown in `dotnet new list` and Visual Studio New Project dialog. |
| `shortName` | string | CLI short name used with `dotnet new <shortName>`. Use lowercase, no spaces. Can be a string or array of strings for aliases. |

## Strongly Recommended Fields

| Field | Type | Description |
|-------|------|-------------|
| `$schema` | string | Always set to `http://json.schemastore.org/template` for editor completions and validation. |
| `author` | string | Template author name. |
| `sourceName` | string | Text replaced in file names and content with the user-provided project name (`-n` / `--name` parameter). Typically set to the project's root namespace. |
| `classifications` | string[] | Tags shown in the **Tags** column of `dotnet new list` and the **All Project Types** dropdown in Visual Studio. Common values: `Console`, `Web`, `Library`, `Desktop`, `Cloud`, `Service`, `IoT`, `Games`, `Mobile`. |
| `tags` | object | Must contain `language` and `type`. See below. |
| `defaultName` | string | Pre-populated project name in Visual Studio. If omitted, `Project1` is used. |
| `preferNameDirectory` | bool | When `true`, creates a directory named after the project. Recommended for project templates. Default: `false`. |
| `description` | string | Human-readable description of the template. |

### tags Object

```json
"tags": {
  "language": "C#",
  "type": "project"
}
```

| Tag | Valid Values | Notes |
|-----|-------------|-------|
| `language` | `C#`, `F#`, `VB` | Required for template to appear in language-filtered views. |
| `type` | `project`, `item`, `solution` | **Critical:** If `type` is missing, the template will NOT appear in Visual Studio New Project dialog. |

---

## Optional Fields

| Field | Type | Description |
|-------|------|-------------|
| `groupIdentity` | string | Groups multiple templates as one entry. Users choose via options (e.g., different frameworks). |
| `precedence` | int | When multiple templates share `groupIdentity`, higher precedence wins. Default: `0`. |
| `guids` | string[] | GUIDs in template source to replace with fresh GUIDs on instantiation. Format and casing are preserved automatically. |
| `sourceName` | string | See Strongly Recommended above. |
| `symbols` | object | Parameters, generated values, computed values. See `symbols-and-parameters.md`. |
| `sources` | object[] | Controls which files to include/exclude and where to place them. See Source Definition below. |
| `primaryOutputs` | object[] | Files for post-action processing (e.g., which project to restore, which file to open in editor). |
| `postActions` | object[] | Actions to run after template creation (restore, run script, open file). See `post-actions-and-constraints.md`. |
| `constraints` | object | Conditions that must be met for the template to be usable (OS, SDK version, workloads). See `post-actions-and-constraints.md`. |
| `forms` | object | Value transforms (regex, replacements) that can be applied to symbol values. See `symbols-and-parameters.md`. |
| `placeholderFilename` | string | Filename used to preserve empty directories in templates. Default: `"-.-"`. |

---

## Source Definition

Controls file inclusion, exclusion, and renaming. If omitted, an implicit source with `"source": "./"` and `"target": "./"` is created.

```json
"sources": [
  {
    "source": "./",
    "target": "./",
    "exclude": [
      "**/[Bb]in/**",
      "**/[Oo]bj/**",
      ".template.config/**/*",
      "**/*.filelist",
      "**/*.user",
      "**/*.lock.json"
    ],
    "modifiers": [
      {
        "condition": "(IncludeTests)",
        "include": ["tests/**"]
      },
      {
        "condition": "(!IncludeTests)",
        "exclude": ["tests/**"]
      }
    ]
  }
]
```

| Property | Default | Description |
|----------|---------|-------------|
| `source` | `./` | Source path relative to `.template.config` parent. |
| `target` | `./` | Output path relative to user's target directory. |
| `include` | `["**/*"]` | Glob patterns for files to include. |
| `exclude` | `["**/[Bb]in/**", "**/[Oo]bj/**", ...]` | Glob patterns to exclude. |
| `copyOnly` | `["**/node_modules/**/*"]` | Files copied without template processing. |
| `rename` | — | Explicit file renames: `{ "old-name.txt": "new-name.txt" }`. |
| `condition` | — | Boolean expression controlling whether this source applies. |
| `modifiers` | — | Array of conditional include/exclude/copyOnly overrides. |

---

## Primary Outputs

Defines files for post-action processing (restore, open in editor, add to solution).

```json
"primaryOutputs": [
  { "path": "MyProject.csproj" },
  {
    "condition": "(HostIdentifier != \"dotnetcli\")",
    "path": "Program.cs"
  }
]
```

| Property | Description |
|----------|-------------|
| `path` | Relative path to the output file (before symbol-based renaming). |
| `condition` | Optional boolean expression. If `false`, this output is excluded. |

---

## Complete Minimal Example

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Your Name",
  "classifications": ["Console"],
  "identity": "YourCompany.ConsoleApp.CSharp",
  "name": "Your Console Application",
  "shortName": "yourconsole",
  "sourceName": "ConsoleApp",
  "defaultName": "ConsoleApp",
  "preferNameDirectory": true,
  "tags": {
    "language": "C#",
    "type": "project"
  }
}
```
