# Example: Solution Template — Web API with Class Library

A complete solution template that produces a multi-project solution with a Web API, a shared class library, and an optional test project.

---

## Directory Structure

```
MyWebSolution/
├── .template.config/
│   ├── template.json
│   └── ide.host.json
├── MyWebSolution.app.sln
├── src/
│   ├── MyWebSolution.Api/
│   │   ├── MyWebSolution.Api.csproj
│   │   ├── Program.cs
│   │   └── Controllers/
│   │       └── WeatherController.cs
│   └── MyWebSolution.Core/
│       ├── MyWebSolution.Core.csproj
│       └── Services/
│           └── WeatherService.cs
└── tests/
    └── MyWebSolution.Tests/
        ├── MyWebSolution.Tests.csproj
        └── WeatherServiceTests.cs
```

---

## .template.config/template.json

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Contoso Ltd",
  "classifications": ["Web", "API", "Solution"],
  "identity": "Contoso.WebApiSolution.CSharp",
  "name": "Contoso Web API Solution",
  "shortName": "contosoapi",
  "sourceName": "MyWebSolution",
  "defaultName": "WebApiSolution",
  "description": "A Web API solution with a shared class library and optional test project.",
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
  "symbols": {
    "Framework": {
      "type": "parameter",
      "description": "The target framework for all projects.",
      "datatype": "choice",
      "choices": [
        { "choice": "net8.0", "description": "Target .NET 8.0 (LTS)" },
        { "choice": "net9.0", "description": "Target .NET 9.0" }
      ],
      "replaces": "net8.0",
      "defaultValue": "net8.0"
    },
    "IncludeTests": {
      "type": "parameter",
      "dataType": "bool",
      "description": "Whether to include the test project.",
      "defaultValue": "true"
    },
    "HttpsPort": {
      "type": "parameter",
      "datatype": "integer",
      "description": "Port for the HTTPS endpoint."
    },
    "HttpsPortGenerated": {
      "type": "generated",
      "generator": "port",
      "parameters": { "low": 44300, "high": 44399 }
    },
    "HttpsPortReplacer": {
      "type": "generated",
      "generator": "coalesce",
      "parameters": {
        "sourceVariableName": "HttpsPort",
        "fallbackVariableName": "HttpsPortGenerated"
      },
      "replaces": "44399"
    },
    "skipRestore": {
      "type": "parameter",
      "datatype": "bool",
      "defaultValue": "false",
      "description": "Skip NuGet restore after creation."
    }
  },
  "sources": [
    {
      "modifiers": [
        {
          "condition": "(!IncludeTests)",
          "exclude": ["tests/**"]
        }
      ]
    }
  ],
  "primaryOutputs": [
    { "path": "src/MyWebSolution.Api/MyWebSolution.Api.csproj" },
    { "path": "src/MyWebSolution.Core/MyWebSolution.Core.csproj" },
    {
      "condition": "(IncludeTests)",
      "path": "tests/MyWebSolution.Tests/MyWebSolution.Tests.csproj"
    }
  ],
  "postActions": [
    {
      "condition": "(!skipRestore)",
      "description": "Restore NuGet packages.",
      "manualInstructions": [{ "text": "Run 'dotnet restore'" }],
      "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
      "continueOnError": true
    }
  ]
}
```

---

## .template.config/ide.host.json

```json
{
  "$schema": "http://json.schemastore.org/vs-2017.3.host",
  "symbolInfo": [
    {
      "id": "Framework",
      "name": { "text": "Target Framework" },
      "isVisible": "true"
    },
    {
      "id": "IncludeTests",
      "name": { "text": "Include Test Project" },
      "isVisible": "true"
    }
  ]
}
```

---

## MyWebSolution.app.sln (excerpt)

The `.sln` file contains the project GUIDs that must be listed in the `guids` array:

```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyWebSolution.Api", "src\MyWebSolution.Api\MyWebSolution.Api.csproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyWebSolution.Core", "src\MyWebSolution.Core\MyWebSolution.Core.csproj", "{22222222-2222-2222-2222-222222222222}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyWebSolution.Tests", "tests\MyWebSolution.Tests\MyWebSolution.Tests.csproj", "{33333333-3333-3333-3333-333333333333}"
EndProject
```

**Important:** The solution file is named `MyWebSolution.app.sln` (not `MyWebSolution.sln`) to avoid the "File Modification Detected" dialog in Visual Studio. The template engine renames it based on the `sourceName` replacement.

---

## Install and Test

```bash
# Install the template
cd MyWebSolution
dotnet new install .\

# Create a solution with tests
dotnet new contosoapi -n OrderService

# Create a solution without tests
dotnet new contosoapi -n OrderService --IncludeTests false --Framework net9.0

# Verify
cd OrderService
dotnet build OrderService.app.sln
dotnet test   # (only if IncludeTests was true)

# Check replacements:
# - All "MyWebSolution" → "OrderService" in namespaces, file names, project names
# - All GUIDs are fresh (not the 11111... placeholders)
# - HTTPS port is a random available port (not 44399)
# - Framework is net9.0 in all .csproj files

# Uninstall
cd ../MyWebSolution
dotnet new uninstall .\
```
