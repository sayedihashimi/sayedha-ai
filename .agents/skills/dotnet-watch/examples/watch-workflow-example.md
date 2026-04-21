# Example: Full `dotnet watch` Workflow

This example demonstrates a complete workflow: start a .NET web API with `dotnet watch`, make code changes, and handle both successful hot-reload and a rude edit.

---

## Scenario

The user has an ASP.NET Core Web API project at `src/WeatherApi/WeatherApi.csproj` and wants to run the app and add a new endpoint.

---

## Step 1: Start the App

**Command (run in async mode):**
```
dotnet watch --non-interactive --project src/WeatherApi/WeatherApi.csproj
```

**Console output:**
```
dotnet watch 🔥 Hot reload enabled. For a list of supported edits, see https://aka.ms/dotnet/hot-reload.
  💡 Press "Ctrl + R" to restart.
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

✅ The app is running and ready.

---

## Step 2: Make a Simple Edit (Hot-Reload Succeeds)

**Action:** Add a new `GET /hello` endpoint to an existing controller by adding a method:

```csharp
[HttpGet("hello")]
public string GetHello() => "Hello from dotnet watch!";
```

**Console output after saving:**
```
dotnet watch ⌚ File changed: ./Controllers/GreetingController.cs
dotnet watch 🔥 Hot reload succeeded.
```

✅ Change is live. The new endpoint is available at `http://localhost:5000/hello`.

---

## Step 3: Make a Breaking Edit (Rude Edit)

**Action:** Add a new middleware class and register it in `Program.cs`:

```csharp
app.UseMiddleware<RequestLoggingMiddleware>();
```

**Console output after saving:**
```
dotnet watch ⌚ File changed: ./Program.cs
dotnet watch ⚠ Unable to apply hot reload, attempting to restart...
dotnet watch 🔧 Restarting the app...
dotnet watch 🔥 Hot reload enabled. For a list of supported edits, see https://aka.ms/dotnet/hot-reload.
  💡 Press "Ctrl + R" to restart.
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

✅ The app automatically restarted with `--non-interactive`. Both the new endpoint and the new middleware are active.

---

## Step 4: Introduce a Build Error

**Action:** Accidentally introduce a syntax error while editing:

```csharp
public string GetHello() => "Hello from dotnet watch!"  // missing semicolon
```

**Console output after saving:**
```
dotnet watch ⌚ File changed: ./Controllers/GreetingController.cs
error CS1002: ; expected [src/WeatherApi/WeatherApi.csproj]
dotnet watch ❌ Build failed. Waiting for file changes...
```

❌ Build failed. Fix the syntax error and save the file.

**After fixing and saving:**
```
dotnet watch ⌚ File changed: ./Controllers/GreetingController.cs
dotnet watch 🔥 Hot reload succeeded.
```

✅ Error resolved, app is back to normal.

---

## Key Takeaways

1. **Always start with `dotnet watch --non-interactive`** — never `dotnet run`.
2. **Wait for confirmation** after every file save before proceeding.
3. **Hot-reload succeeded** = safe to continue.
4. **Rude edit + `--non-interactive`** = automatic restart; wait for startup messages.
5. **Build failed** = fix the error and save; `dotnet watch` retries automatically.
