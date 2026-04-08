# ide.host.json Reference

The `ide.host.json` file configures how a .NET template appears and behaves in Visual Studio. It is placed alongside `template.json` inside the `.template.config/` directory.

**Schema:** `http://json.schemastore.org/vs-2017.3.host`

---

## File Location

```
MyTemplate/
└── .template.config/
    ├── template.json
    ├── ide.host.json
    └── icon.png          (optional)
```

---

## Fields

### icon

Specifies a custom icon displayed in the Visual Studio New Project dialog.

```json
{
  "$schema": "http://json.schemastore.org/vs-2017.3.host",
  "icon": "icon.png"
}
```

- The icon file must be in the `.template.config/` directory.
- Recommended size: 64×64 pixels or larger (PNG format).
- If omitted, Visual Studio uses a default template icon.

### symbolInfo

Controls which template parameters are visible and how they appear in the Visual Studio New Project dialog. Each entry maps to a symbol defined in `template.json`.

```json
{
  "$schema": "http://json.schemastore.org/vs-2017.3.host",
  "symbolInfo": [
    {
      "id": "Framework",
      "name": {
        "text": "Target Framework"
      },
      "isVisible": "true"
    },
    {
      "id": "AuthorName",
      "name": {
        "text": "Author Name"
      },
      "isVisible": "true"
    },
    {
      "id": "Description",
      "name": {
        "text": "Project Description"
      },
      "isVisible": "true"
    }
  ]
}
```

| Property | Type | Description |
|----------|------|-------------|
| `id` | string | Must match the symbol name in `template.json`. |
| `name.text` | string | Label displayed in the Visual Studio UI. |
| `isVisible` | string | `"true"` or `"false"`. Controls whether the parameter appears in the UI. |

**Important:** For parameters to appear in Visual Studio, you typically need a `Framework` symbol defined in `template.json` — this is a known requirement for parameter visibility.

### unsupportedHosts

Hides the template from specific host applications. Use when a template should only work from the CLI and not from Visual Studio, or vice versa.

```json
{
  "$schema": "http://json.schemastore.org/vs-2017.3.host",
  "unsupportedHosts": [
    { "id": "vs" }
  ]
}
```

| Host ID | Application |
|---------|-------------|
| `vs` | Visual Studio |
| `vs-mac` | Visual Studio for Mac |
| `dotnetcli` | .NET CLI (`dotnet new`) |

---

## Complete Example

A full `ide.host.json` for a project template with an icon, visible parameters, and no host restrictions:

```json
{
  "$schema": "http://json.schemastore.org/vs-2017.3.host",
  "icon": "icon.png",
  "symbolInfo": [
    {
      "id": "Framework",
      "name": {
        "text": "Target Framework"
      },
      "isVisible": "true"
    },
    {
      "id": "AuthorName",
      "name": {
        "text": "Author Name"
      },
      "isVisible": "true"
    }
  ]
}
```

---

## Tips

- Always include the `$schema` field for editor completions.
- The `symbolInfo` array only controls visibility — the actual parameter behavior is defined in `template.json`.
- If your template works only from the CLI (e.g., it uses `dotnet run` post-actions), add `"unsupportedHosts": [{ "id": "vs" }]`.
- Icon files are included in NuGet packages when you pack the template.
