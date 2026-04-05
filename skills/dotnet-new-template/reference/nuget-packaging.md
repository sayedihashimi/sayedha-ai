# NuGet Template Packaging

Guide for packaging .NET templates as NuGet packages for distribution.

---

## Overview

Template packages are NuGet packages (`.nupkg`) containing one or more templates. Users install them with `dotnet new install <PackageId>` and they appear in both `dotnet new list` and Visual Studio.

---

## Step 1: Create the Template Pack Project

Use the built-in `templatepack` template to scaffold a packaging project:

```bash
dotnet new install Microsoft.TemplateEngine.Authoring.Templates
dotnet new templatepack -n "YourCompany.Templates"
```

This creates a `.csproj` with the correct settings. Alternatively, create manually:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- NuGet package metadata -->
    <PackageId>YourCompany.Templates</PackageId>
    <PackageVersion>1.0.0</PackageVersion>
    <Title>Your Company Templates</Title>
    <Authors>Your Name</Authors>
    <Description>Custom .NET templates for Your Company projects.</Description>
    <PackageTags>dotnet-new;templates;yourcompany</PackageTags>
    <PackageProjectUrl>https://github.com/yourcompany/templates</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>

    <!-- Template package settings -->
    <PackageType>Template</PackageType>
    <TargetFramework>net8.0</TargetFramework>
    <IncludeContentInPack>true</IncludeContentInPack>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <ContentTargetFolders>content</ContentTargetFolders>
    <NoWarn>$(NoWarn);NU5128</NoWarn>
    <NoDefaultExcludes>true</NoDefaultExcludes>
  </PropertyGroup>

  <PropertyGroup>
    <LocalizeTemplates>false</LocalizeTemplates>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.TemplateEngine.Tasks"
                      Version="*"
                      PrivateAssets="all"
                      IsImplicitlyDefined="true" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="content\**\*" Exclude="content\**\bin\**;content\**\obj\**" />
    <Compile Remove="**\*" />
  </ItemGroup>

  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

### Critical Properties

| Property | Value | Why |
|----------|-------|-----|
| `PackageType` | `Template` | **Required.** Makes the package discoverable via `dotnet new search`. |
| `IncludeContentInPack` | `true` | Includes template files in the package. |
| `IncludeBuildOutput` | `false` | Prevents compiling template source as a library. |
| `ContentTargetFolders` | `content` | Places templates in the `content/` folder inside the .nupkg. |
| `NoDefaultExcludes` | `true` | Includes dotfiles (`.gitignore`, `.editorconfig`) in the package. |

---

## Step 2: Organize Template Content

Place all templates in a `content/` directory:

```
YourCompany.Templates/
├── YourCompany.Templates.csproj
├── README.md
└── content/
    ├── console-app/
    │   ├── .template.config/
    │   │   ├── template.json
    │   │   └── ide.host.json
    │   ├── ConsoleApp.csproj
    │   └── Program.cs
    └── class-library/
        ├── .template.config/
        │   ├── template.json
        │   └── ide.host.json
        ├── ClassLibrary.csproj
        └── Class1.cs
```

Each subfolder under `content/` is a separate template (identified by its `.template.config/template.json`).

---

## Step 3: Build the Package

```bash
dotnet pack
```

The `.nupkg` file is created in `bin/Release/`:
```
bin/Release/YourCompany.Templates.1.0.0.nupkg
```

---

## Step 4: Test Locally

```bash
# Install from local .nupkg
dotnet new install .\bin\Release\YourCompany.Templates.1.0.0.nupkg

# Verify templates are listed
dotnet new list

# Test each template
dotnet new yourconsole -n TestProject
cd TestProject && dotnet build

# Uninstall
dotnet new uninstall YourCompany.Templates
```

---

## Step 5: Publish to NuGet.org

```bash
# Push to nuget.org (requires API key)
dotnet nuget push bin/Release/YourCompany.Templates.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json

# Users can then install with:
dotnet new install YourCompany.Templates
```

---

## Versioning

Follow [Semantic Versioning](https://semver.org/):

- **Major** version: Breaking changes to template structure or parameters
- **Minor** version: New templates added, new optional parameters
- **Patch** version: Bug fixes, documentation updates

Update `<PackageVersion>` in the `.csproj` before each release.

Users update installed templates:
```bash
dotnet new update                              # Check for updates
dotnet new install YourCompany.Templates::2.0  # Install specific version
```

---

## Searching for Templates

Published templates can be discovered:

```bash
# Search by name
dotnet new mytemplate --search

# Search by tag
dotnet new --search --tag web
```

---

## Common Issues

| Issue | Fix |
|-------|-----|
| Template not found after install | Verify `.template.config/template.json` exists inside the .nupkg (it's a zip file). |
| Template not in `dotnet new search` | Ensure `<PackageType>Template</PackageType>` is set. |
| Dotfiles missing from package | Add `<NoDefaultExcludes>true</NoDefaultExcludes>`. |
| Build errors from template source code | Add `<Compile Remove="**\*" />` to prevent compilation. |
