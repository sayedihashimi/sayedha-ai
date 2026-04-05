<#
.SYNOPSIS
    Validates a .NET template's template.json file for common issues.

.DESCRIPTION
    Checks a .NET template's template.json for required fields, recommended fields,
    naming conventions, and common mistakes. Reports errors and warnings.

.PARAMETER TemplatePath
    Path to the template folder (containing .template.config/) or directly to a template.json file.

.EXAMPLE
    .\Validate-DotnetTemplate.ps1 -TemplatePath C:\MyTemplate

.EXAMPLE
    .\Validate-DotnetTemplate.ps1 -TemplatePath C:\MyTemplate\.template.config\template.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$TemplatePath
)

$ErrorActionPreference = 'Stop'

# --- Resolve the template.json path ---
$templateJsonPath = $null
if (Test-Path $TemplatePath -PathType Leaf) {
    if ((Split-Path $TemplatePath -Leaf) -eq 'template.json') {
        $templateJsonPath = $TemplatePath
    }
    else {
        Write-Error "File '$TemplatePath' is not a template.json file."
        exit 1
    }
}
elseif (Test-Path $TemplatePath -PathType Container) {
    $candidate = Join-Path $TemplatePath '.template.config' 'template.json'
    if (Test-Path $candidate) {
        $templateJsonPath = $candidate
    }
    else {
        Write-Error "No .template.config/template.json found in '$TemplatePath'."
        exit 1
    }
}
else {
    Write-Error "Path '$TemplatePath' does not exist."
    exit 1
}

$templateJsonPath = (Resolve-Path $templateJsonPath).Path
$templateConfigDir = Split-Path $templateJsonPath -Parent
$templateRoot = Split-Path $templateConfigDir -Parent

Write-Host "`n  Validating: $templateJsonPath`n" -ForegroundColor Cyan

# --- Parse JSON ---
try {
    $json = Get-Content $templateJsonPath -Raw | ConvertFrom-Json
}
catch {
    Write-Host "  ERROR: Failed to parse template.json as valid JSON." -ForegroundColor Red
    Write-Host "  $_" -ForegroundColor Red
    exit 1
}

$errors = @()
$warnings = @()

# --- Required fields ---
$requiredFields = @('identity', 'name', 'shortName')
foreach ($field in $requiredFields) {
    if (-not $json.PSObject.Properties[$field] -or [string]::IsNullOrWhiteSpace($json.$field)) {
        $errors += "Missing required field: '$field'"
    }
}

# --- Strongly recommended fields ---
$recommendedFields = @('author', 'sourceName', 'classifications', 'description')
foreach ($field in $recommendedFields) {
    if (-not $json.PSObject.Properties[$field] -or
        ($json.$field -is [string] -and [string]::IsNullOrWhiteSpace($json.$field)) -or
        ($json.$field -is [array] -and $json.$field.Count -eq 0)) {
        $warnings += "Missing recommended field: '$field'"
    }
}

# --- $schema field ---
if (-not $json.PSObject.Properties['$schema']) {
    $warnings += "Missing '`$schema' field. Recommended: 'http://json.schemastore.org/template'"
}

# --- defaultName ---
if (-not $json.PSObject.Properties['defaultName']) {
    $warnings += "Missing 'defaultName' field. Visual Studio will use 'Project1' as default."
}

# --- tags validation ---
if (-not $json.PSObject.Properties['tags']) {
    $errors += "Missing 'tags' object. Must contain 'language' and 'type'."
}
else {
    $tags = $json.tags
    if (-not $tags.PSObject.Properties['language'] -or [string]::IsNullOrWhiteSpace($tags.language)) {
        $errors += "Missing 'tags.language'. Set to 'C#', 'F#', or 'VB'."
    }
    if (-not $tags.PSObject.Properties['type'] -or [string]::IsNullOrWhiteSpace($tags.type)) {
        $errors += "Missing 'tags.type'. Must be 'project', 'item', or 'solution'."
    }
    elseif ($tags.type -notin @('project', 'item', 'solution')) {
        $errors += "Invalid 'tags.type': '$($tags.type)'. Must be 'project', 'item', or 'solution'."
    }
}

# --- shortName conventions ---
if ($json.PSObject.Properties['shortName'] -and -not [string]::IsNullOrWhiteSpace($json.shortName)) {
    $shortName = if ($json.shortName -is [array]) { $json.shortName[0] } else { $json.shortName }
    if ($shortName -cmatch '[A-Z]') {
        $warnings += "shortName '$shortName' contains uppercase characters. Convention is lowercase."
    }
    if ($shortName -match '\s') {
        $errors += "shortName '$shortName' contains spaces. Spaces are not allowed."
    }
    if ($shortName.Length -gt 30) {
        $warnings += "shortName '$shortName' is very long ($($shortName.Length) chars). Keep it short for CLI usability."
    }
}

# --- sourceName check (project templates) ---
if ($json.PSObject.Properties['tags'] -and $json.tags.PSObject.Properties['type']) {
    $templateType = $json.tags.type
    if ($templateType -eq 'project' -and
        (-not $json.PSObject.Properties['sourceName'] -or [string]::IsNullOrWhiteSpace($json.sourceName))) {
        $warnings += "Project template missing 'sourceName'. Without it, namespace/filename replacement won't work."
    }
}

# --- symbols validation ---
if ($json.PSObject.Properties['symbols']) {
    $symbols = $json.symbols
    foreach ($prop in $symbols.PSObject.Properties) {
        $symbol = $prop.Value
        $symbolName = $prop.Name

        if (-not $symbol.PSObject.Properties['type']) {
            $errors += "Symbol '$symbolName' is missing 'type' field."
            continue
        }

        if ($symbol.type -eq 'parameter' -and $symbol.PSObject.Properties['datatype']) {
            if ($symbol.datatype -eq 'choice') {
                if (-not $symbol.PSObject.Properties['choices'] -or $symbol.choices.Count -eq 0) {
                    $errors += "Choice parameter '$symbolName' has no 'choices' defined."
                }
            }
        }

        if ($symbol.type -eq 'generated') {
            if (-not $symbol.PSObject.Properties['generator']) {
                $errors += "Generated symbol '$symbolName' is missing 'generator' field."
            }
        }
    }
}
else {
    $warnings += "No 'symbols' defined. Consider adding parameters for Framework, author, etc."
}

# --- ide.host.json check ---
$ideHostPath = Join-Path $templateConfigDir 'ide.host.json'
if (-not (Test-Path $ideHostPath)) {
    $warnings += "No 'ide.host.json' found. Template parameters won't be visible in Visual Studio UI."
}

# --- primaryOutputs check for solution templates ---
if ($json.PSObject.Properties['tags'] -and
    $json.tags.PSObject.Properties['type'] -and
    $json.tags.type -eq 'solution' -and
    -not $json.PSObject.Properties['primaryOutputs']) {
    $warnings += "Solution template missing 'primaryOutputs'. Visual Studio may not load projects correctly."
}

# --- guids check for solution templates ---
if ($json.PSObject.Properties['tags'] -and
    $json.tags.PSObject.Properties['type'] -and
    $json.tags.type -eq 'solution') {
    $slnFiles = Get-ChildItem -Path $templateRoot -Filter '*.sln' -Recurse -ErrorAction SilentlyContinue
    if ($slnFiles.Count -gt 0 -and (-not $json.PSObject.Properties['guids'] -or $json.guids.Count -eq 0)) {
        $warnings += "Solution template has .sln file but no 'guids' array. Project GUIDs won't be regenerated."
    }
}

# --- Report results ---
Write-Host "  ── Results ──`n" -ForegroundColor White

if ($errors.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host "  ✅ No issues found. Template looks good!`n" -ForegroundColor Green
    exit 0
}

foreach ($err in $errors) {
    Write-Host "  ❌ ERROR: $err" -ForegroundColor Red
}
foreach ($warn in $warnings) {
    Write-Host "  ⚠️  WARN:  $warn" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  Summary: $($errors.Count) error(s), $($warnings.Count) warning(s)" -ForegroundColor White

if ($errors.Count -gt 0) {
    Write-Host "  Status: FAIL — fix errors before using this template.`n" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "  Status: PASS with warnings — consider addressing warnings.`n" -ForegroundColor Yellow
    exit 0
}
