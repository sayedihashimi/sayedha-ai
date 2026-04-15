# Example: Project Template — Console Application

A complete, realistic project template for a console application with framework selection and author parameters.

---

## Directory Structure

```
MyConsoleTool/
├── .template.config/
│   ├── template.json
│   └── ide.host.json
├── MyConsoleTool.csproj
├── Program.cs
└── README.md
```

---

## .template.config/template.json

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Contoso Ltd",
  "classifications": ["Console", "Common"],
  "identity": "Contoso.ConsoleTool.CSharp",
  "name": "Contoso Console Tool",
  "shortName": "contool",
  "sourceName": "MyConsoleTool",
  "defaultName": "MyConsoleTool",
  "description": "A console application template with framework selection and author configuration.",
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
        { "choice": "net8.0", "description": "Target .NET 8.0 (LTS)" },
        { "choice": "net9.0", "description": "Target .NET 9.0" }
      ],
      "replaces": "net8.0",
      "defaultValue": "net8.0"
    },
    "AuthorName": {
      "type": "parameter",
      "datatype": "text",
      "description": "The author name for the project.",
      "replaces": "AuthorName",
      "defaultValue": "Your Name"
    },
    "Description": {
      "type": "parameter",
      "datatype": "text",
      "description": "A short description of the project.",
      "replaces": "ProjectDescription",
      "defaultValue": "A console application."
    },
    "copyrightYear": {
      "type": "generated",
      "generator": "now",
      "replaces": "2024",
      "parameters": {
        "format": "yyyy"
      }
    },
    "skipRestore": {
      "type": "parameter",
      "datatype": "bool",
      "description": "Skip automatic NuGet restore after project creation.",
      "defaultValue": "false"
    }
  },
  "primaryOutputs": [
    { "path": "MyConsoleTool.csproj" }
  ],
  "postActions": [
    {
      "condition": "(!skipRestore)",
      "description": "Restore NuGet packages required by this project.",
      "manualInstructions": [{ "text": "Run 'dotnet restore'" }],
      "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
      "continueOnError": true
    },
    {
      "condition": "(HostIdentifier != \"dotnetcli\")",
      "description": "Opens Program.cs in the editor.",
      "manualInstructions": [],
      "actionId": "84C0DA21-51C8-4541-9940-6CA19AF04EE6",
      "args": { "files": "0" },
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
      "id": "AuthorName",
      "name": { "text": "Author Name" },
      "isVisible": "true"
    },
    {
      "id": "Description",
      "name": { "text": "Project Description" },
      "isVisible": "true"
    }
  ]
}
```

---

## MyConsoleTool.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Authors>AuthorName</Authors>
    <Description>ProjectDescription</Description>
    <Copyright>Copyright © 2024 AuthorName</Copyright>
  </PropertyGroup>

</Project>
```

---

## Program.cs

```csharp
// Copyright © 2024 AuthorName
// ProjectDescription

namespace MyConsoleTool;

class Program
{
    static async Task Main(string[] args)
    {
        await Console.Out.WriteLineAsync("Hello from MyConsoleTool!");
    }
}
```

---

## Install and Test

```bash
# Install the template from the project folder
cd MyConsoleTool
dotnet new install .\

# Create a new project from the template
cd ..
dotnet new contool -n AwesomeTool --Framework net9.0 --AuthorName "Jane Smith"

# Verify
cd AwesomeTool
dotnet build
dotnet run
# Output: Hello from AwesomeTool!

# Check that replacements worked:
# - File: AwesomeTool.csproj (not MyConsoleTool.csproj)
# - Namespace: AwesomeTool (not MyConsoleTool)
# - Author: Jane Smith (not AuthorName)
# - Framework: net9.0 (not net8.0)
# - Year: current year (not 2024)

# Uninstall
cd ../MyConsoleTool
dotnet new uninstall .\
```
