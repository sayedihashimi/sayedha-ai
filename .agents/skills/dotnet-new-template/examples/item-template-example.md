# Example: Item Template — Service Class

A complete item template that generates a C# class file with configurable class name and namespace.

---

## Directory Structure

```
service-class/
├── .template.config/
│   └── template.json
└── ServiceClass.cs
```

Item templates are simpler than project templates — they typically produce one or a few files added to an existing project.

---

## .template.config/template.json

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Contoso Ltd",
  "classifications": ["Common", "Code"],
  "identity": "Contoso.ServiceClass.CSharp",
  "name": "Contoso Service Class",
  "shortName": "service",
  "description": "A service class with dependency injection constructor and interface.",
  "tags": {
    "language": "C#",
    "type": "item"
  },
  "symbols": {
    "ClassName": {
      "type": "parameter",
      "datatype": "text",
      "description": "The name of the service class.",
      "replaces": "ServiceClass",
      "fileRename": "ServiceClass",
      "defaultValue": "ServiceClass"
    },
    "includeInterface": {
      "type": "parameter",
      "datatype": "bool",
      "description": "Whether to include an interface for the service.",
      "defaultValue": "true"
    }
  }
}
```

---

## ServiceClass.cs

```csharp
using Microsoft.Extensions.Logging;

namespace MyProject;

//#if (includeInterface)
public interface IServiceClass
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public class ServiceClass : IServiceClass
//#else
public class ServiceClass
//#endif
{
    private readonly ILogger<ServiceClass> _logger;

    public ServiceClass(ILogger<ServiceClass> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ServiceClass executing at: {Time}", DateTimeOffset.Now);
        await Task.CompletedTask;
    }
}
```

---

## How It Works

### ClassName parameter

The `ClassName` parameter uses both `replaces` and `fileRename`:
- **`replaces: "ServiceClass"`** — Every occurrence of `ServiceClass` in the file content (class name, constructor, logger type, interface name) is replaced with the user's value.
- **`fileRename: "ServiceClass"`** — The filename `ServiceClass.cs` is renamed (e.g., `OrderProcessor.cs`).

### includeInterface parameter

The `includeInterface` boolean parameter uses conditional processing:
- When `true` (default): The interface and `: IServiceClass` inheritance are included.
- When `false`: Only the plain class is generated.

---

## Install and Test

```bash
# Install the template
cd service-class
dotnet new install .\

# Navigate to an existing project
cd ../MyExistingProject

# Create a service class with default name
dotnet new service

# Create with a custom name and no interface
dotnet new service --ClassName OrderProcessor --includeInterface false

# Check the result:
# - File: OrderProcessor.cs (renamed from ServiceClass.cs)
# - Class: OrderProcessor (replaced ServiceClass)
# - Logger: ILogger<OrderProcessor>
# - No interface generated

# View available parameters
dotnet new service -?

# Uninstall
cd ../service-class
dotnet new uninstall .\
```

---

## Key Differences from Project Templates

| Aspect | Project Template | Item Template |
|--------|-----------------|---------------|
| `tags.type` | `project` | `item` |
| `sourceName` | Set (for namespace/file replacement) | Usually not needed (use `ClassName` parameter instead) |
| `preferNameDirectory` | `true` | Not applicable |
| `defaultName` | Set to a meaningful default | Not applicable |
| `primaryOutputs` | Lists the .csproj file | Usually omitted |
| Typical output | Full project with .csproj | One or a few code files |
| Visual Studio | New Project dialog | Add > New Item (not always shown) |
