# AGENTS.md

Guidance for AI coding agents working on this repository.

## Project

**HRCounter** is a Beat Saber mod (BSIPA plugin) that displays the player's real-time heart rate during gameplay. Author: qe201020335. Source: https://github.com/qe201020335/HRCounter. Current target Beat Saber version is in `HRCounter/manifest.json`.

The mod ingests heart rate from many possible sources, broadcasts the value through a small in-game pipeline, and renders it either via a Counters+ canvas integration or a standalone world-space counter. It also records HR into BeatLeader replays and plays it back on replay viewing.

## Tech stack

- **Language**: C# (see `<LangVersion>` in `HRCounter/HRCounter.csproj`), targets `net472`. The actual runtime is Unity's bundled Mono, not Microsoft .NET Framework — so `HRCounter.csproj` sets `<FrameworkPathOverride>$(BeatSaberDir)\Beat Saber_Data\Managed</FrameworkPathOverride>` to compile against the Mono framework DLLs the game ships with. This avoids runtime surprises from API differences between Mono and the real .NET Framework. Don't disable that override.
- **Plugin framework**: BSIPA + SiraUtil. Versions pinned in `HRCounter/manifest.json` (`dependsOn`).
- **DI**: Zenject (Beat Saber bundles it), wired via SiraUtil installers.
- **UI**: BeatSaberMarkupLanguage (BSML) — optional; the mod degrades gracefully without it.
- **Optional integrations**: Counters+, BeatLeader, ScoreSaber, YUR.
- **JSON**: Newtonsoft.Json. Custom converters live in `HRCounter/Utils/Converters/`.
- **Asset bundle**: counter prefab is built in the separate `HRCounterBundle/` Unity project and shipped as `HRCounter/Resources/hrcounter`.

## Branching

- `master` — last public release. Often outdated because only stable, non-prerelease code lands here.
- `dev` — **default working branch** for in-progress prerelease work.
- Feature branches are cut from `dev` (e.g. `pulsoid`, `bsml`, `websocket`, `1.42`).
- PRs target `dev`, not `master`.

## Build / run

Standard BSIPA mod build:

1. Restore NuGet packages — SDK references game assemblies via `BeatSaberModdingTools.Tasks` / `BeatSaberDir`.
2. Build the `HRCounter` project. Output goes to the configured Beat Saber install.
3. Launch Beat Saber; logs land in `Logs/_latest.log` under the install root.
4. The asset bundle is regenerated from `HRCounterBundle/` (Unity project) via the `AssetBundleExporter` editor script and copied into `HRCounter/Resources/hrcounter`.

## Sub-projects in the repo

These live alongside the main `HRCounter/` plugin project but build/deploy independently.

### `HRCounterBundle/`

Unity project that produces the counter prefab asset bundle. The Unity version is pinned to match Beat Saber's runtime — see `HRCounterBundle/ProjectSettings/ProjectVersion.txt`. Don't upgrade Unity casually.

- Contains `Assets/HRCounter.prefab` (the in-game counter layout — TextMeshPro number + heart icon + replay icon), the custom font (`Heartbit-Bold SDF`), and embedded TextMesh Pro shaders.
- `Assets/Editor/AssetBundleExporter.cs` is the editor script that builds the bundle. Output is the binary file copied to `HRCounter/Resources/hrcounter` and embedded in the plugin DLL as a manifest resource.
- Loaded at runtime by `AssetBundleManager`. Re-export the bundle whenever the prefab layout, font, or shaders change.

### `ConfigGenerator/`

React + TypeScript web app (Create React App style, `react-scripts`) for generating the mod's JSON config file outside the game. Uses Fluent UI components. Versions are in `ConfigGenerator/package.json`.

- Entry: `src/index.tsx` → `src/App.tsx` → `src/Components/Main.tsx`.
- Models in `src/models/` mirror the C# config / data source shape so users can build a config without launching the game.
- Helpers: `src/utils/Generator.ts`, `FileSaver.ts`, `EncodingHelper.ts`, `CryptHelper.ts`, `GameSettingsController.ts`.
- Standard CRA scripts: `npm start`, `npm run build`, `npm test`. Not bundled into the plugin — deployed separately as a static site.

### `hrcounter-proxy/`

Cloudflare Worker (TypeScript, Wrangler) that proxies the HypeRate WebSocket for the in-game `HypeRate2` data source.

- Entry: `src/index.ts`. HypeRate logic in `src/hyperate/handler.ts` and `src/hyperate/HypeRate.ts`. Auth in `src/auth.ts` (validates Beat Saber platform user / Steam / Oculus).
- The proxy exists so the mod doesn't ship HypeRate API credentials and so protocol changes can be patched server-side without a mod update. The `HypeRate2` source connects to `wss://hrcounter.skyqe.net/proxy/hyperate`.
- Scripts: `npm run dev` (local Wrangler), `npm run deploy` (push to Cloudflare), `npm test` (Vitest with `@cloudflare/vitest-pool-workers`).
- Config in `wrangler.jsonc`. Type definitions auto-generated via `npm run cf-typegen`.

## Architecture

### Live HR data flow

```
[HR device]
    └─> [DataSource impl: Pulsoid2/HypeRate2/YUR/HTTP/OSC/...]
          └─> IHRDataSource.OnHRDataReceived event
                └─> HRDataManager (marshals to main thread, broadcasts)
                      ├─> LiveHRProvider (caches current HR, fires HRChanged)
                      │     └─> HRCounter (abstract base) → display
                      └─> GamePauseController (auto-pause if HR ≥ threshold)
                      └─> ReplayHRRecorder (samples for BeatLeader replay)
```

### Replay HR flow

```
BeatLeader replay custom data ("HeartBeatQuest")
    └─> ReplayHRDataConverter.Decode() → ReplayHRData (sorted array)
          └─> ReplayHRProvider (ITickable, advances by AudioTimeSyncController.songTime)
                └─> HRCounter → display
```

`IInGameHRProvider` is the polymorphic interface; both `LiveHRProvider` and `ReplayHRProvider` implement it so the counter doesn't care which mode it's in.

### Display

- `HRCounter` (abstract) — owns shared setup/teardown, HR text update, color from `RenderUtils.DetermineColor`.
- `HRCounterCountersPlus` — `ICounter` integration; positions inside the Counters+ canvas.
- `HRCounterStandalone` — `IInitializable`/`IDisposable` world-space counter; supports 360/90 mode by attaching to `FlyingGameHUDRotation.Container`.
- The asset bundle prefab is instantiated by `AssetBundleManager.SetupCustomCounter(isReplay)` and returns a `CustomCounter` struct (`Canvas`, `Container`, `Numbers`).
- Unity scene teardown is enough — **do not call `Object.Destroy` in `IDisposable.Dispose`** for in-game GameObjects. They die with the scene.

### Pulsoid integration (recent, well-developed)

- `Integrations/Pulsoid/` is a self-contained subsystem.
- **Transport**: `PulsoidOAuthClient` (oauth2/) and `PulsoidApiClient` (api/v1/) wrap `HttpClient`.
- **Domain**: `PulsoidAuthenticator` is a stateless singleton (DI-bound `AsSingle`) that owns both clients. Three operations:
  - `AuthenticateAsync(Action<string> onVerificationUriReceived, CancellationToken)` — full OAuth 2.0 device flow. Caller's callback opens the browser; method polls until success/denied/timeout/cancelled.
  - `ValidateTokenAsync(token, CancellationToken)` — returns `TokenValidationResult` with `Valid` / `NotFound` / `Expired` / `Failure` / `Cancelled`.
  - `RevokeTokenAsync(token, CancellationToken)` — returns bool.
- **Result types**: `Results/AuthResult.cs` (consolidated), `Results/TokenValidationResult.cs`. Methods catch all exceptions internally and never throw — callers branch on `result.Result`.
- **Models**: paired `*Response.cs` / `*ErrorResponse.cs` for each endpoint, in `Models/`. Error responses use C# 14 primary constructors with `[method: JsonConstructor]` so the parsed enum is computed at deserialization time.
- The actual data source that consumes the token is `Pulsoid2.cs` (a `WebSocketSource` subclass at `wss://pulsoid.net/api/v1/data/real_time?response_mode=text_plain_only_heart_rate`). The legacy `Pulsoid.cs` HTTP polling source is kept around but marked `[Obsolete("Use Pulsoid2 instead.", true)]`.

## Key directories

| Path | Purpose |
|---|---|
| `HRCounter/Plugin.cs` | BSIPA entry point. Detects optional deps, installs Zenject installers at App/Menu/Player scopes. |
| `HRCounter/Configuration/PluginConfig.cs` | BSIPA config object (`INotifyPropertyChanged`). Hot-reloads. |
| `HRCounter/Installers/` | Zenject installers per scope: `AppInstaller` (singletons + servers + Pulsoid auth), `MenuInstaller` (chains to `BSMLInstaller` if BSML is installed), `BSMLInstaller` (BSML view controllers + flow coordinator), `GameplayHeartRateInstaller` (data source + `HRDataManager`), `GameplayCoreInstaller` (HR provider + standalone counter), `GamePauseInstaller`, `ReplayRecorderInstaller`. |
| `HRCounter/Data/` | `HRDataManager`, `BPM`, `IHRDataSource`, `IInGameHRProvider`, replay subsystem. |
| `HRCounter/Data/DataSources/` | All HR source implementations. `Base/DataSource.cs` and `Base/WebSocketSource.cs` are the abstract bases. |
| `HRCounter/Integrations/Pulsoid/` | OAuth2 device flow + API + domain logic for Pulsoid. |
| `HRCounter/Web/` | `SimpleHttpServer`, `SimpleOscServer`, `SimpleWebSocketClient` — local servers for the HTTP/OSC data sources. |
| `HRCounter/UI/` | BSML view controllers and config menu. `BSML/*.bsml` for layouts. |
| `HRCounter/Utils/` | `Extensions`, `RenderUtils`, `UserInfoHelper`, `DataSourceUtils`, generic `Utils`, and JSON converters under `Converters/`. |
| `HRCounter/Resources/hrcounter` | Compiled asset bundle (binary). |
| `HRCounterBundle/` | Standalone Unity project that builds the asset bundle. |

Loose files in the project root (`HRCounter/`): `Plugin.cs`, `HRCounter.cs` (abstract base), `HRCounterStandalone.cs`, `HRCounterCountersPlus.cs`, `AssetBundleManager.cs`, `IconManager.cs`, `GamePauseController.cs`.

## Code conventions

- New code: `internal` by default; `public` only when external assemblies need it (BSIPA `Plugin`, Zenject `Installer`s, BSIPA config types, BSML view controllers, Counters+ feature targets, etc.). Older code in the repo is more uniformly `public`; don't churn it just to match the new convention.
- Nullable reference types are **on**. Use `null!` initializer for fields that DI / framework will populate; use `?` for genuinely nullable.
- File-scoped namespaces.
- Constants: `SCREAMING_SNAKE_CASE` for `private const`.
- Fields: `_camelCase` private; `PascalCase` properties.
- No comments unless they explain non-obvious *why*. Don't restate code.
- No emojis in any output unless the user explicitly asks.
- Don't add backwards-compat shims, dead code, or speculative abstractions.

## Logger injection

`AppInstaller.cs` binds `Logger` two ways:

- **Catch-all (no ID)**: any `[Inject] Logger _logger` field on a class in this assembly gets a child logger named after the concrete `context.ObjectType.Name`. Guarded by `ShouldBindLogger` which checks `context.ObjectType.Assembly == _pluginMetadata.Assembly` so it doesn't hijack other mods' logger injections.
- **Per-base-class IDs**: for abstract base classes that need a stable logger name across all subclasses, the installer has explicit `Container.Bind<Logger>().WithId(typeof(X)).FromMethod(CreateChildLogger).AsTransient()` lines. Currently bound: `BaseConfigViewController`, `DataSource`, `HRProxyBase`, `WebSocketSource`, `HRCounter`. The class then injects `[Inject(Id = typeof(X))] private readonly Logger _logger`.

`CreateChildLogger` derives the name from `context.Identifier as Type` first, else `context.ObjectType` (concrete type). Loggers are cached per name in `_loggers`.

**When adding a new abstract base class that needs a stable logger name**: add a new `Container.Bind<Logger>().WithId(typeof(YourBase)).FromMethod(CreateChildLogger).AsTransient();` line in `InstallBindings`. Don't try to auto-discover them via reflection — that path was attempted and reverted because handling assembly type-load failures cleanly was too messy.

## Adding a new HR data source

1. Subclass `DataSource` (HTTP polling) or `WebSocketSource` (websocket).
2. Implement `Start`/`Stop`, parse incoming HR, call `OnHeartRateDataReceived(hr)`.
3. Register it in `DataSourceManager` with `RegisterDataSource<YourSource>(KEY, sourceLinkTextCallback, precondition)`. Signature is `(string key, Func<Task<string>> sourceLinkTextCallback, Func<bool> precondition)` (sync `Func<string>` overload also exists). The precondition is `Func<bool>` and returns whether the source can run (token set, dependency installed, etc.); the link-text callback returns a status/info string shown in the data source info panel.
4. Add a config field if needed in `PluginConfig`.
5. Add a UI block in `UI/BSML/dataSource.bsml` and wire it in `DataSourceMenu.cs`.

### Planned rewrite

`DataSourceManager` is slated for a full rewrite. The current static registry is fine for in-tree sources but doesn't support external mods adding their own. The new design will:

- Replace static `RegisterDataSource<T>(...)` calls with a per-source **descriptor class** owned by each source (precondition, status text, type info, etc.) that other mods can also instantiate and register.
- Add a **status-change event** on each descriptor so UI (`DataSourceMenu`, info panels) can subscribe and refresh automatically instead of polling or relying on config-property change forwarding.

When working in this area, prefer changes that don't entrench the static-registry assumption — e.g., don't add more `static DataSourceInfo Foo = RegisterDataSource(...)` lines than necessary, and avoid building UI logic that depends on the registry being static.

## Async / cancellation patterns

- Long-running async work that may be cancelled by user action: hold a `CancellationTokenSource` on the controller, expose a `Cancel*` method that cancels & disposes it. Do this in `DidDeactivate`.
- For domain methods that wrap network calls (see `PulsoidAuthenticator`), prefer the **never-throw** pattern: catch `OperationCanceledException` and `Exception` inside, return a result object with a `ResultType` enum. Callers branch on the enum and never need try/catch.
- For UI updates from background tasks, use `IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew`.
- `Process.Start` for opening URLs: use `new ProcessStartInfo { FileName = url, Verb = "open" }`. Validate `https` if the URL came from an external service.

## Configuration

`PluginConfig` is BSIPA-managed and instance-shared. All UI bindings flow through it via `INotifyPropertyChanged`. Hot reload of the config file is supported. Don't cache config values in long-lived fields — read from the property each time so changes take effect.

## UI view controller convention

View controllers extend `BaseConfigViewController`. It hooks `Config.PropertyChanged` while active and forwards the event directly:

```csharp
private void OnConfigChanged(object? sender, PropertyChangedEventArgs args)
{
    NotifyPropertyChanged(args.PropertyName);
    // ...subclass hook
}
```

**Important convention**: when a `[UIValue("X")]` property is backed by a config property, **name the BSML key identically to the config property name**. The forwarded `NotifyPropertyChanged(args.PropertyName)` then lights up the BSML binding automatically with no per-property switch in the controller.

Example (matches existing code style — the codebase uses string literals, not `nameof(...)`):
```csharp
[UIValue("StreamerMode")]  // matches PluginConfig.StreamerMode
public bool StreamerMode
{
    get => Config.StreamerMode;
    set => Config.StreamerMode = value;
}
```

Only override the per-property `OnConfigChanged(string propertyName)` hook in the subclass when the change requires extra work beyond a simple BSML notify (refreshing derived text, kicking off network calls, etc.).

For UI state that isn't config-backed (button enabled state, status text, modal text), use plain `[UIValue("...")]` properties and call `NotifyPropertyChanged()` from the setter — the `field` keyword keeps the boilerplate minimal.

## Memory file

A user-level memory note exists: **GameObjects don't need explicit `Object.Destroy` in `Dispose`** for in-game scene objects — Unity scene unload cleans up. Only flag missing destroys for objects that outlive the scene (DontDestroyOnLoad, app-scope singletons).

## Common pitfalls

- **`Plugin.Instance.UserAgent`** is the canonical UA for HTTP/WS clients. Always set `_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Plugin.Instance.UserAgent)`.
- **Abstract base classes that need a stable-name logger** must use the ID-based pattern above (declare `[Inject(Id = typeof(Base))] Logger` and add the matching `WithId` line in `AppInstaller`). Don't rely on `context.MemberInfo` to derive the declaring type — Zenject's `InjectContext` doesn't expose it.
- **Field initializers vs constructors**: when a base class needs `Plugin.Instance` data, prefer field initializer (`= Plugin.Instance.UserAgent`) over a constructor — keeps subclasses constructor-free.
- **`field` keyword** is used in BSML view controllers for auto-property setters that need to call `NotifyPropertyChanged()`. Requires the `LangVersion` set in `HRCounter.csproj`; don't downgrade it.
- **`ReadAsStringAsync`** does **not** accept a `CancellationToken` on .NET Framework 4.7.2. Don't try to pass one.
- **`init` accessors** are unavailable on net472. Use `set` with `private`/internal access if you want immutability.
- **`Object.Destroy(prefabInstance)`** must be called by whoever creates a transient prefab when the operation fails (e.g. `HRCounter.Setup()` destroys the canvas if `SetupCounter` returns false).

## Don't

- Don't write `README.md` or other docs unless explicitly requested.
- Don't introduce `init`-only properties (net472).
- Don't add `Object.Destroy` to `IDisposable.Dispose` for scene objects.
- Don't bypass git hooks or sign-off requirements.
- Don't try to auto-discover ID-based logger bindings via reflection; that was reverted. Add `WithId` lines manually when introducing a new base class.
- Don't add backwards-compatibility shims or "removed" comments — delete cleanly.
- Don't add speculative abstractions for hypothetical future sources/displays. Three similar lines beats a premature framework.
