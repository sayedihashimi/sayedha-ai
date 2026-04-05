# Solution (Multi-Project) Templates

Guide for creating templates that produce an entire solution with multiple projects.

---

## Key Differences from Project Templates

| Aspect | Project Template | Solution Template |
|--------|-----------------|-------------------|
| `tags.type` | `project` | `solution` |
| `.template.config` location | Project root | Solution root |
| `primaryOutputs` | Usually omitted | Lists all project files |
| `guids` | Rarely needed | Lists all project GUIDs from .sln |
| Solution file | Not included | Included in template |
| `editorTreatAs` tag | Not needed | Set to `solution` |

---

## Directory Structure

The `.template.config/` folder must be at the solution root (same level as or parent of the `.sln` file):

```
MySolution/
├── .template.config/
│   ├── template.json
│   └── ide.host.json
├── MySolution.app.sln          ← Note: NOT named MySolution.sln
├── src/
│   ├── WebApp/
│   │   ├── WebApp.csproj
│   │   └── Program.cs
│   └── WebApp.Core/
│       ├── WebApp.Core.csproj
│       └── CoreService.cs
└── tests/
    └── WebApp.Tests/
        ├── WebApp.Tests.csproj
        └── CoreServiceTests.cs
```

---

## Solution Naming Best Practice

**Critical:** The solution file name must NOT match `sourceName` or `defaultName`.

If `sourceName` is `MySolution`, do **not** name the file `MySolution.sln`. Instead use a different name like `MySolution.app.sln`, then apply a rename:

```json
"sourceName": "MySolution",
"symbols": {
  "solutionRename": {
    "type": "generated",
    "generator": "regex",
    "datatype": "string",
    "parameters": {
      "source": "name",
      "steps": [{ "regex": "(.+)", "replacement": "$1" }]
    },
    "fileRename": "MySolution.app"
  }
}
```

If the solution filename matches `sourceName`, Visual Studio may show an unexpected "File Modification Detected" dialog immediately after creation.

---

## template.json for Solution Templates

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Your Company",
  "classifications": ["Web", "Solution"],
  "identity": "YourCompany.WebSolution.CSharp",
  "name": "Your Web Solution",
  "shortName": "yourwebsln",
  "sourceName": "MySolution",
  "defaultName": "WebSolution",
  "preferNameDirectory": true,
  "tags": {
    "language": "C#",
    "type": "solution",
    "editorTreatAs": "solution"
  },
  "guids": [
    "11111111-1111-1111-1111-111111111111",
    "22222222-2222-2222-2222-222222222222",
    "33333333-3333-3333-3333-333333333333"
  ],
  "primaryOutputs": [
    { "path": "src/WebApp/WebApp.csproj" },
    { "path": "src/WebApp.Core/WebApp.Core.csproj" },
    {
      "condition": "(IncludeTests)",
      "path": "tests/WebApp.Tests/WebApp.Tests.csproj"
    }
  ],
  "symbols": {
    "IncludeTests": {
      "type": "parameter",
      "dataType": "bool",
      "defaultValue": "true",
      "description": "Whether to include the test project."
    }
  },
  "sources": [{
    "modifiers": [
      {
        "condition": "(!IncludeTests)",
        "exclude": ["tests/**"]
      }
    ]
  }],
  "postActions": [
    {
      "description": "Restore NuGet packages.",
      "manualInstructions": [{ "text": "Run 'dotnet restore'" }],
      "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
      "continueOnError": true
    }
  ]
}
```

---

## GUID Replacement

Every project in a `.sln` file has a unique GUID. These must be replaced with fresh GUIDs when instantiating the template.

1. Open your `.sln` file and find all project GUIDs:
   ```
   Project("{FAE04EC0-...}") = "WebApp", "src\WebApp\WebApp.csproj", "{11111111-1111-1111-1111-111111111111}"
   ```

2. List each project GUID in the `guids` array of `template.json`.

3. The template engine automatically generates new GUIDs and replaces all occurrences (in the `.sln` file and any other files referencing them), preserving the original format and casing.

---

## primaryOutputs

For solution templates, `primaryOutputs` lists the project files that Visual Studio should load. This is critical for:
- Visual Studio to open the correct projects after creation
- Post-actions (like NuGet restore) to know which projects to process

If `primaryOutputs` is incorrect, projects will not load in Visual Studio after creation.

---

## editorTreatAs Tag

Add `"editorTreatAs": "solution"` to the `tags` object. This tells Visual Studio to treat the template output as a solution rather than a single project:

```json
"tags": {
  "language": "C#",
  "type": "solution",
  "editorTreatAs": "solution"
}
```

---

## Testing Solution Templates

```bash
# Install the template
dotnet new install .\

# Create from template
dotnet new yourwebsln -n TestSolution

# Verify
cd TestSolution
dotnet build TestSolution.app.sln
dotnet test

# Clean up
dotnet new uninstall .\
```
