# `dotnet watch` Console Output Patterns

This reference documents the key console output messages from `dotnet watch --non-interactive` that Copilot should monitor when running .NET applications.

---

## Startup Messages

These indicate the app and `dotnet watch` have initialized:

```
dotnet watch 🔥 Hot reload enabled. For a list of supported edits, see https://aka.ms/dotnet/hot-reload.
  💡 Press "Ctrl + R" to restart.
```

For web apps, also look for:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

For console apps, look for your app's expected initial output after the hot-reload enabled message.

---

## File Change Detection

When a source file is saved, `dotnet watch` reports:

```
dotnet watch ⌚ File changed: ./Path/To/ChangedFile.cs
```

This means `dotnet watch` has detected the change and is beginning to process it. **Do not assume the change is applied yet.**

---

## Hot-Reload Success

```
dotnet watch 🔥 Hot reload succeeded.
```

This is the **primary success signal**. When you see this, the code change is live in the running app. It is safe to proceed with testing or further edits.

---

## Build Errors

When a code change introduces a compilation error:

```
dotnet watch ❌ Build failed. Waiting for file changes...
```

Or detailed compiler errors like:

```
error CS1002: ; expected
error CS0246: The type or namespace name 'Foo' could not be found
```

**Action:** Fix the compilation error in the source file and save again. `dotnet watch` will automatically retry.

---

## Rude Edits (Hot-Reload Not Possible)

Some edits cannot be applied via hot-reload. These are called "rude edits." Examples include:
- Adding or removing method parameters
- Changing base classes or interfaces
- Adding new project references
- Modifying `Program.cs` or startup configuration

With `--non-interactive`, `dotnet watch` automatically restarts the app:

```
dotnet watch ⚠ Unable to apply hot reload, attempting to restart...
dotnet watch 🔧 Restarting the app...
```

Followed by a full restart sequence (build → start → startup messages again).

**Action:** Wait for the app to fully restart. Look for the startup messages (see above) before proceeding.

---

## App Crash or Unhandled Exception

If the app crashes:

```
dotnet watch ❌ Application shut down unexpectedly.
dotnet watch ⌚ Waiting for file changes...
```

**Action:** Check the error output, fix the issue, and save the file. `dotnet watch` will rebuild and restart automatically.

---

## Summary Table

| Pattern | Meaning | Next Step |
|---------|---------|-----------|
| `Hot reload enabled` | App started, watching for changes | Proceed with edits |
| `Now listening on:` | Web app is ready | Proceed with requests/edits |
| `File changed:` | Change detected, processing | Wait for result |
| `Hot reload succeeded` | Change applied successfully | Safe to proceed |
| `Build failed` | Compilation error | Fix and save again |
| `Unable to apply hot reload` | Rude edit detected | Wait for auto-restart |
| `Restarting the app` | App is restarting | Wait for startup messages |
| `Application shut down unexpectedly` | App crashed | Fix and save again |
