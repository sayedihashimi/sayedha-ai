# Symbols and Parameters Reference

Symbols define template parameters, computed values, and dynamic replacements. They are the primary mechanism for making templates configurable.

---

## Symbol Types Overview

| Type | Purpose | User Input? |
|------|---------|-------------|
| `parameter` | User-provided value (CLI flag or UI input) | Yes |
| `generated` | Computed by a built-in generator | No |
| `computed` | Boolean expression based on other symbols | No |
| `derived` | Transformed from another symbol's value | No |
| `bind` | Value provided by the host environment | No |

---

## Parameter Symbols

The most common symbol type. Users provide values via `dotnet new <template> --<ParameterName> <value>` or through Visual Studio UI.

### Data Types

| datatype | Description | Example |
|----------|-------------|---------|
| `bool` | Boolean (`true`/`false`) | Enable/disable a feature |
| `choice` | Enumeration of predefined values | Framework selection |
| `text` / `string` | Free-form text | Author name, namespace |
| `int` / `integer` | 64-bit signed integer | Port number |
| `float` | Double-precision float | Version number |
| `hex` | Hexadecimal number | Color codes |

### Key Properties

| Property | Description |
|----------|-------------|
| `replaces` | Text in template content to replace with this parameter's value. |
| `fileRename` | Text in filenames to replace with this parameter's value. |
| `defaultValue` | Value used when user doesn't provide one. |
| `defaultIfOptionWithoutValue` | Value used when flag is present but no value given. |
| `description` | Help text shown in `dotnet new <template> -?`. |
| `isRequired` | If `true` (or condition evaluating to true), user must provide value. |
| `isEnabled` | Condition controlling whether parameter is active. |

### Boolean Parameter

```json
"EnableLogging": {
  "type": "parameter",
  "dataType": "bool",
  "defaultValue": "false",
  "description": "Whether to include logging infrastructure."
}
```

Use in template files with conditional processing:
```csharp
//#if (EnableLogging)
builder.AddLogging();
//#endif
```

### Choice Parameter

```json
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
}
```

### Text Parameter with replaces and fileRename

```json
"ClassName": {
  "type": "parameter",
  "datatype": "text",
  "description": "The name of the class.",
  "replaces": "MyClass",
  "fileRename": "MyClass",
  "defaultValue": "MyClass"
}
```

This replaces `MyClass` in both file contents and filenames. If the user passes `--ClassName Widget`, then `MyClass.cs` becomes `Widget.cs` and all occurrences of `MyClass` in code become `Widget`.

### Multi-Choice Parameter

```json
"Platform": {
  "type": "parameter",
  "datatype": "choice",
  "allowMultipleValues": true,
  "choices": [
    { "choice": "Windows", "description": "Windows Desktop" },
    { "choice": "Linux", "description": "Linux" },
    { "choice": "MacOS", "description": "macOS" }
  ],
  "defaultValue": "Windows|Linux"
}
```

Usage: `dotnet new mytemplate --Platform Windows --Platform Linux`

---

## Generated Symbols

Values computed by built-in generators. No user input.

### Available Generators

| Generator | Description |
|-----------|-------------|
| `casing` | Convert another symbol to upper/lower case. |
| `coalesce` | Use primary value if set, otherwise fallback (like C# `??`). |
| `constant` | Fixed value. |
| `port` | Random available port number. |
| `guid` | Generate a new GUID. |
| `now` | Current date/time. |
| `random` | Random integer in a range. |
| `regex` | Regex-based transformation of another symbol. |
| `regexMatch` | Boolean: does another symbol match a regex? |
| `switch` | Switch/case logic returning a value. |
| `join` | Concatenate multiple symbols. |

### Port Generation (Web Projects)

A common pattern uses three symbols — user parameter, generated port, and coalesce:

```json
"HttpsPort": {
  "type": "parameter",
  "datatype": "integer",
  "description": "Port number for the HTTPS endpoint in launchSettings.json."
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
  "replaces": "44345"
}
```

### Casing

```json
"nameLower": {
  "type": "generated",
  "generator": "casing",
  "parameters": { "source": "ownerName", "toLower": true },
  "replaces": "ownername_lower"
}
```

### Current Date

```json
"copyrightYear": {
  "type": "generated",
  "generator": "now",
  "replaces": "1975",
  "parameters": { "format": "yyyy" }
}
```

### Switch

```json
"targetFrameworkMoniker": {
  "type": "generated",
  "generator": "switch",
  "datatype": "string",
  "replaces": "FrameworkTFM",
  "parameters": {
    "evaluator": "C++",
    "cases": [
      { "condition": "(Framework == 'net8.0')", "value": "net8.0" },
      { "condition": "(Framework == 'net9.0')", "value": "net9.0" }
    ]
  }
}
```

---

## Computed Symbols

Boolean values computed from other symbols. Useful for conditional content.

```json
"IsWebProject": {
  "type": "computed",
  "value": "(Framework == \"net8.0\" && IncludeWeb)"
}
```

Evaluator options: `C++2` (default), `C++`, `MSBUILD`, `VB`.

---

## Derived Symbols

Transform another symbol's value using a form (defined in the `forms` section).

```json
"symbols": {
  "appNameLower": {
    "type": "derived",
    "valueSource": "name",
    "valueTransform": "lowerCaseForm",
    "replaces": "app_name_lower"
  }
},
"forms": {
  "lowerCaseForm": {
    "identifier": "lowerCase"
  }
}
```

---

## Conditional Content

Control which files or code sections are included based on symbol values.

### C# / JSON Style (Default)

```csharp
//#if (EnableAuth)
services.AddAuthentication();
//#endif
```

### XML / HTML Style

```xml
<!--#if (IncludeDocker) -->
<DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
<!--#endif -->
```

### Conditional File Inclusion

Use `sources.modifiers` in template.json:

```json
"sources": [{
  "modifiers": [
    {
      "condition": "(!IncludeTests)",
      "exclude": ["**/*.Tests/**"]
    }
  ]
}]
```

---

## Value Forms

Transform symbol values with built-in or custom forms.

### Built-in Forms

| Form | Description |
|------|-------------|
| `identity` | No change (passthrough). |
| `lowerCase` | Convert to lowercase. |
| `upperCase` | Convert to uppercase. |
| `lowerCaseInvariant` | Invariant culture lowercase. |
| `upperCaseInvariant` | Invariant culture uppercase. |
| `firstLowerCase` | Lowercase first character only. |
| `firstUpperCase` | Uppercase first character only. |
| `titleCase` | Capitalize first letter of each word. |
| `kebabCase` | Convert to kebab-case. |
| `xmlEncode` | XML-encode special characters. |
| `jsonEncode` | JSON-encode special characters. |

### Custom Regex Form

```json
"forms": {
  "stripDots": {
    "identifier": "replace",
    "pattern": "\\.",
    "replacement": ""
  }
}
```

### Applying Forms to sourceName

```json
"sourceName": "MyProject",
"forms": {
  "kebab": { "identifier": "kebabCase" }
},
"symbols": {
  "projectNameKebab": {
    "type": "derived",
    "valueSource": "name",
    "valueTransform": "kebab",
    "replaces": "my-project"
  }
}
```
