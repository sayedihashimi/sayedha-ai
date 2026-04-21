---
name: dotnet-watch
description: "Runs .NET apps with dotnet watch --non-interactive instead of dotnet run to avoid file locks and enable hot-reload. Use when running, debugging, or iterating on any .NET application project. Triggers on: dotnet run, run the app, start the app, launch the project, .NET hot-reload, dotnet watch."
---

# Running .NET Apps with `dotnet watch`

## Core Purpose

Use `dotnet watch --non-interactive` instead of `dotnet run` when running .NET applications. This avoids file-lock issues and enables automatic hot-reload when source files change, so the app stays running across code edits without manual restarts.

---

## When to Activate

Use this workflow whenever you need to **run a .NET application** — any project that would normally be launched with `dotnet run`. This includes console apps, web apps, APIs, Blazor apps, and worker services.

**Do NOT use** for class libraries, test projects, or `dotnet build`-only scenarios.

---

## Workflow: Starting the App

1. **Launch with `dotnet watch`.**
   Run the app in async mode so you can continue working while it runs:
   ```
   dotnet watch --non-interactive --project <path-to-project>
   ```
   - Always use `--non-interactive` to prevent interactive prompts that block automation.
   - Use `--project` to specify the project if you are not in the project directory.
   - Start the process in **async mode** (background) so you can edit files while the app runs.

2. **Wait for startup confirmation.**
   Read the console output and look for indicators that the app has started successfully. See `reference/dotnet-watch-output.md` for the full list of output patterns. Key startup signals:
   - Web apps: `Now listening on: http://localhost:<port>`
   - Console apps: your app's expected initial output
   - All apps: `dotnet watch 🔥 Hot reload enabled.` or similar hot-reload readiness message

3. **Verify the app is running.**
   Confirm the process is alive and producing expected output before proceeding to edits.

---

## Workflow: Editing Code While the App Runs

4. **Edit source files normally.**
   Save your changes. `dotnet watch` monitors file changes automatically.

5. **Wait for hot-reload to complete.**
   After saving, read the console output from the `dotnet watch` process. Wait for one of these outcomes:

   | Outcome | Console Output | Action |
   |---------|---------------|--------|
   | **Success** | `Hot reload succeeded.` | Continue — changes are live. |
   | **Build error** | `Build failed.` or compiler error messages | Fix the error and save again. |
   | **Rude edit** | `Unable to apply hot reload... Restarting...` | Wait for the app to restart automatically (`--non-interactive` handles this). Then wait for the startup confirmation again (step 2). |

6. **Do NOT proceed until you see a clear success or restart-complete signal.**
   Hot-reload may take several seconds. Be patient and read the output before making further changes or verifying behavior.

---

## Key Flags and Options

| Flag | Purpose |
|------|---------|
| `--non-interactive` | **Required.** Disables interactive prompts. On rude edits, automatically restarts instead of asking. |
| `--project <path>` | Specify the project file if not in the project directory. |
| `--no-hot-reload` | Disable hot-reload (falls back to full rebuild + restart on every change). Rarely needed. |
| `-- --urls http://localhost:5000` | Pass arguments through to the underlying app (e.g., set URL for web apps). |

---

## Important Behavior Notes

- **Rude edits** are changes that cannot be applied via hot-reload (e.g., changing method signatures, adding new classes, modifying startup code). With `--non-interactive`, `dotnet watch` automatically restarts the app when a rude edit is detected — no manual intervention needed. Wait for the restart to complete before continuing.
- **File locks:** Unlike `dotnet run`, `dotnet watch` does not hold long-lived file locks on source files, making it safe to edit files while the app runs.
- **Multiple projects:** If the solution has multiple runnable projects, launch each in a separate async session.

---

## Inline Example

**Scenario:** Run a web API and then add a new endpoint.

**Step 1 — Launch:**
```
dotnet watch --non-interactive --project src/MyApi/MyApi.csproj
```
Wait for output: `Now listening on: http://localhost:5000`

**Step 2 — Edit:** Add a new controller file `WeatherController.cs`.

**Step 3 — Verify:** Read `dotnet watch` output. See:
```
dotnet watch ⌚ File changed: ./Controllers/WeatherController.cs
dotnet watch 🔥 Hot reload succeeded.
```
Changes are live — proceed to test the new endpoint.

---

## Reference Files

| File | Use When |
|------|----------|
| `reference/dotnet-watch-output.md` | Looking up console output patterns for success, errors, or rude edits |

## Examples

| File | Description |
|------|-------------|
| `examples/watch-workflow-example.md` | Full end-to-end workflow: start app, edit code, handle hot-reload |
