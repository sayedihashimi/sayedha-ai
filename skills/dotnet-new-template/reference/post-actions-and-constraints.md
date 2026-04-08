# Post-Actions and Constraints Reference

Post-actions run after template instantiation. Constraints control when a template is available.

---

## Post-Actions

Post-actions are defined in the `postActions` array in `template.json`. Each post-action has:

| Property | Required | Description |
|----------|----------|-------------|
| `actionId` | Yes | GUID identifying the post-action type. |
| `description` | No | Human-readable description. |
| `condition` | No | Boolean expression — action runs only if true. |
| `continueOnError` | No | If `true`, subsequent actions run even if this one fails. Default: `false`. |
| `manualInstructions` | No | Fallback instructions if action can't run automatically. |
| `args` | Varies | Action-specific arguments. |

### Available Post-Actions

#### Restore NuGet Packages

**ActionId:** `210D431B-A78B-4D2F-B762-4ED3E3EA9025`

Restores NuGet packages for projects listed in `primaryOutputs`. Ignored in Visual Studio (it restores automatically).

```json
"primaryOutputs": [
  { "path": "MyProject.csproj" }
],
"postActions": [{
  "condition": "(!skipRestore)",
  "description": "Restore NuGet packages.",
  "manualInstructions": [{ "text": "Run 'dotnet restore'" }],
  "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
  "continueOnError": true
}]
```

#### Run Script

**ActionId:** `3A7C4B45-1F5D-4A30-959A-51B88E82B5D2`

Runs an executable after template creation. Working directory is the template output root.

```json
"postActions": [{
  "actionId": "3A7C4B45-1F5D-4A30-959A-51B88E82B5D2",
  "args": {
    "executable": "setup.cmd",
    "args": "",
    "redirectStandardOutput": false,
    "redirectStandardError": false
  },
  "manualInstructions": [{ "text": "Run 'setup.cmd'" }],
  "continueOnError": false,
  "description": "Run project setup script"
}]
```

#### Open File in Editor

**ActionId:** `84C0DA21-51C8-4541-9940-6CA19AF04EE6`

Opens a file in Visual Studio after creation. Ignored in CLI.

```json
"primaryOutputs": [
  { "path": "MyProject.csproj" },
  { "path": "Program.cs" }
],
"postActions": [{
  "condition": "(HostIdentifier != \"dotnetcli\")",
  "description": "Opens Program.cs in the editor",
  "manualInstructions": [],
  "actionId": "84C0DA21-51C8-4541-9940-6CA19AF04EE6",
  "args": { "files": "1" },
  "continueOnError": true
}]
```

The `files` value is a semicolon-delimited list of indexes into `primaryOutputs` (0-based).

#### Add Package/Project Reference

**ActionId:** `B17581D1-C5C9-4489-8F0A-004BE667B814`

Adds a NuGet package or project reference.

```json
"postActions": [{
  "description": "Add Serilog NuGet package",
  "actionId": "B17581D1-C5C9-4489-8F0A-004BE667B814",
  "continueOnError": false,
  "manualInstructions": [{ "text": "Run 'dotnet add package Serilog'" }],
  "args": {
    "referenceType": "package",
    "reference": "Serilog",
    "version": "3.1.1",
    "projectFileExtensions": ".csproj"
  }
}]
```

#### Add Projects to Solution

**ActionId:** `D396686C-DE0E-4DE6-906D-291CD29FC5DE`

Adds project files to the nearest solution file. Ignored in Visual Studio.

```json
"postActions": [{
  "description": "Add projects to solution",
  "manualInstructions": [{ "text": "Add generated project to solution manually." }],
  "actionId": "D396686C-DE0E-4DE6-906D-291CD29FC5DE",
  "args": {
    "primaryOutputIndexes": "0;1",
    "solutionFolder": "src"
  },
  "continueOnError": true
}]
```

#### Change File Permissions (Unix/macOS)

**ActionId:** `CB9A6CF3-4F5C-4860-B9D2-03A574959774`

Runs `chmod` on specified files.

```json
"postActions": [{
  "condition": "(OS != \"Windows_NT\")",
  "description": "Make scripts executable",
  "manualInstructions": [{ "text": "Run 'chmod +x *.sh'" }],
  "actionId": "CB9A6CF3-4F5C-4860-B9D2-03A574959774",
  "args": { "+x": "*.sh" },
  "continueOnError": true
}]
```

#### Display Manual Instructions

**ActionId:** `AC1156F7-BB77-4DB8-B28F-24EEBCCA1E5C`

Prints instructions to the console after template creation.

```json
"postActions": [{
  "description": "Next steps",
  "manualInstructions": [{ "text": "Open the solution and configure your database connection string." }],
  "actionId": "AC1156F7-BB77-4DB8-B28F-24EEBCCA1E5C",
  "args": {
    "executable": "dotnet",
    "args": "ef database update"
  },
  "continueOnError": true
}]
```

---

## Constraints

Constraints define conditions that must be met for a template to be usable. Available since .NET SDK 7.0.100.

If constraints are not met, the template is installed but hidden from `dotnet new list` by default.

### Operating System

Restricts to specific OS:

```json
"constraints": {
  "windows-only": {
    "type": "os",
    "args": "Windows"
  }
}
```

Valid values: `Windows`, `Linux`, `OSX`. Can be an array: `["Windows", "Linux"]`.

### Template Engine Host

Restricts to specific applications and versions:

```json
"constraints": {
  "cli-only": {
    "type": "host",
    "args": [{
      "hostname": "dotnetcli",
      "version": "[8.0,)"
    }]
  }
}
```

Host identifiers: `dotnetcli`, `vs`, `vs-mac`, `ide`.

Version ranges use [NuGet version range syntax](https://docs.microsoft.com/en-us/nuget/concepts/package-versioning):
- `[8.0,)` — 8.0 or later
- `[8.0, 9.0)` — 8.0 up to (but not including) 9.0
- `8.0.*` — any 8.0.x version

### Current SDK Version

Restricts to specific .NET SDK versions:

```json
"constraints": {
  "lts-only": {
    "type": "sdk-version",
    "args": ["8.0.*", "6.0.*"]
  }
}
```

### Installed Workloads

Requires specific workloads:

```json
"constraints": {
  "maui-required": {
    "type": "workload",
    "args": "maui"
  }
}
```

### Project Capabilities

Restricts item templates to projects with specific capabilities:

```json
"constraints": {
  "csharp-project": {
    "type": "project-capability",
    "args": "CSharp & TestContainer"
  }
}
```
