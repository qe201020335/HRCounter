# AGENTS.md

Guidance for AI coding agents working on this repository.

## Project

**HRCounter** is a Beat Saber mod (BSIPA plugin) that displays the player's real-time heart rate during gameplay. Author: qe201020335. Source: https://github.com/qe201020335/HRCounter. Current target Beat Saber version is in `HRCounter/manifest.json`.

The mod ingests heart rate from many possible sources, broadcasts the value through a small in-game pipeline, and renders it either via a Counters+ canvas integration or a standalone world-space counter. It also records HR into BeatLeader replays and plays it back on replay viewing.

## Tech stack

- **Language**: C# (see `<LangVersion>` in `HRCounter/HRCounter.csproj`), targets `net472`. The actual runtime is Unity's bundled Mono, not Microsoft .NET Framework — so `HRCounter.csproj` sets `<FrameworkPathOverride>$(BeatSaberDir)\Beat Saber_Data\Managed</FrameworkPathOverride>` to compile against the Mono framework DLLs the game ships with. This avoids runtime surprises from API differences between Mono and the real .NET Framework. Don't disable that override.
- **Plugin framework**: BSIPA + SiraUtil. Versions pinned in `HRCounter/manifest.json` (`dependsOn`).
- **DI**: Zenject (Beat Saber bundles it), wired via SiraUtil installers.
- **UI**: BeatSaberMarkupLanguage (BSML).
- **Localization**: BGLib.Polyglot — Beat Saber's bundled, modified fork of the [Polyglot](https://github.com/agens-no/PolyglotUnity) Unity localization library (CSV-driven, same `Localization.Get` API). Strings live in `HRCounter/Resources/Localization.csv`, injected into the game's Polyglot at load via `Patches/LocalizationPatch.cs`. See the Localization section below.
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

The asset bundle (`HRCounter/Resources/hrcounter`) is **not** built as part of the C# project. It is committed to the repo and only rebuilt manually from the `HRCounterBundle/` Unity project when the prefab/font/shaders change. See the `HRCounterBundle/` section below.

## Sub-projects in the repo

These live alongside the main `HRCounter/` plugin project but build/deploy independently.

### `HRCounterBundle/`

Unity project that produces the counter prefab asset bundle. The Unity version is pinned to match Beat Saber's runtime — see `HRCounterBundle/ProjectSettings/ProjectVersion.txt`. Don't upgrade Unity casually.

- Contains `Assets/HRCounter.prefab` (the in-game counter layout — TextMeshPro number + heart icon + replay icon), the custom font (`Heartbit-Bold SDF`), and embedded TextMesh Pro shaders.
- `Assets/Editor/AssetBundleExporter.cs` is the editor script that builds the bundle.
- **Manual build only.** Open the project in Unity, run the exporter, and copy/commit the output to `HRCounter/Resources/hrcounter`. The C# build does not invoke Unity. Only re-export when the prefab layout, font, or shaders change.
- Loaded at runtime by `AssetBundleManager` from the embedded manifest resource.

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

| Path | Purpose                                                                                                                                                                                                                                                                                                         |
|---|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `HRCounter/Plugin.cs` | BSIPA entry point. Detects optional deps, installs Zenject installers at App/Menu/Player scopes.                                                                                                                                                                                                                |
| `HRCounter/Configuration/PluginConfig.cs` | BSIPA config object (`INotifyPropertyChanged`). Hot-reloads.                                                                                                                                                                                                                                                    |
| `HRCounter/Installers/` | Zenject installers per scope: `AppInstaller` (singletons + servers + Pulsoid auth), `MenuInstaller` (BSML view controllers + flow coordinator), `GameplayHeartRateInstaller` (data source + `HRDataManager`), `GameInstaller` (HR provider + standalone counter), `GamePauseInstaller`, `ReplayRecorderInstaller`. |
| `HRCounter/Data/` | `HRDataManager`, `BPM`, `IHRDataSource`, `IInGameHRProvider`, `IDataSourceDescriptor`, `DataSourceManager`, replay subsystem.                                                                                                                                                                                   |
| `HRCounter/Data/DataSources/` | All HR source implementations. `Base/DataSource.cs` and `Base/WebSocketSource.cs` are the abstract bases.                                                                                                                                                                                                       |
| `HRCounter/Data/SourceDescriptors/` | Per-source `IDataSourceDescriptor<T>` implementations registered with `DataSourceManager`. `SimpleSourceDescriptor<T>` covers the "label + config field" sources; the rest are bespoke (Pulsoid token validation, HTTP/OSC server status, FPS debug).                                                           |
| `HRCounter/Integrations/Pulsoid/` | OAuth2 device flow + API + domain logic for Pulsoid.                                                                                                                                                                                                                                                            |
| `HRCounter/Web/` | `SimpleHttpServer`, `SimpleOscServer`, `SimpleWebSocketClient` — local servers for the HTTP/OSC data sources.                                                                                                                                                                                                   |
| `HRCounter/UI/` | BSML view controllers and config menu. `BSML/*.bsml` for layouts.                                                                                                                                                                                                                                               |
| `HRCounter/Utils/` | `Extensions`, `RenderUtils`, `UserInfoHelper`, `DataSourceUtils`, generic `Utils`, and JSON converters under `Converters/`.                                                                                                                                       |
| `HRCounter/Patches/` | Harmony patches. `LocalizationPatch.cs` injects the localization CSV into the game's Polyglot.                                                                                                                                                                                                                  |
| `HRCounter/Resources/Localization.csv` | Polyglot localization strings.                                                                                                                                                                                                                                                                                  |
| `HRCounter/Resources/hrcounter` | Compiled asset bundle (binary).                                                                                                                                                                                                                                                                                 |
| `HRCounterBundle/` | Standalone Unity project that builds the asset bundle.                                                                                                                                                                                                                                                          |

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

`DataSourceManager` is a DI-bound singleton (`AppInstaller`). Each source is paired with an `IDataSourceDescriptor<TSource>` that owns the key, precondition, status text, and a `StatusChanged` event the UI subscribes to. In-tree sources register in `DataSourceManager.RegisterInternalDataSources`; external mods inject the manager and call the same `RegisterDataSource` overloads from their own installer.

1. Write the source: subclass `DataSource` (HTTP polling) or `WebSocketSource` (websocket), implement `Start`/`Stop`, parse incoming HR, call `OnHeartRateDataReceived(hr)`.
2. Pick a registration form (in increasing order of effort — only reach for a custom descriptor when the simpler options don't fit):
   - **Callback overload** — `RegisterDataSource<TSource>(key, getStatusText, precondition, name?, nameKey?)`. `getStatusText` is either `Func<string>` or `Func<CancellationToken, Task<string>>`. Wrapped internally in a `GenericSourceDescriptor<T>`. Use when the status text is essentially static and there's no need to push updates to the UI — no `StatusChanged` is ever raised. `name`/`nameKey` are optional display-name inputs (see Localization).
   - **`SimpleSourceDescriptor<TSource>`** — `RegisterDataSource(new SimpleSourceDescriptor<TSource>(key, labelKey, () => config.Field, config, nameof(config.Field), name?, nameKey?))`. `labelKey` is a **Polyglot key** (resolved at status-fetch time, not construction). The descriptor subscribes to `PluginConfig.PropertyChanged` for the named field and to streamer-mode changes, redacts the value when streamer mode is on, and raises `StatusChanged`. Use for "label + single config string" sources.
   - **Custom `IDataSourceDescriptor<TSource>`** — write your own when you need to react to non-config events (server status, network validation result, FPS counter, etc.), pull in DI dependencies, or run as a `MonoBehaviour`. Register with `RegisterDataSource<TDesc, TSource>()` (Zenject instantiates the descriptor — and creates a GameObject if `TDesc` is a `MonoBehaviour`) or `RegisterDataSource(instance)` if you constructed it yourself. Implement `IDataSourceDescriptor<T>`, not the non-generic base — `DataSourceType` comes from the generic default impl and is intentionally `internal`. If the descriptor owns resources or event subscriptions that need cleanup, implement `IDisposableSourceDescriptor<T>` instead (same interface, plus `IDisposable`) — `DataSourceManager` will call `Dispose` on teardown. For `MonoBehaviour` descriptors, do the cleanup in `OnDestroy`, not `Dispose`: Zenject destroys the host GameObject during ProjectContext teardown before `DataSourceManager.Dispose` runs, so `Dispose` would fire after the MB is already dead. Members:
     - `Key` — stable identifier stored in `PluginConfig.DataSource`.
     - `Name` / `NameKey` — display name inputs (see Localization). Return null on both to fall back to `Key`. `DataSourceUtils.GetDisplayName` resolves them (`NameKey` localized → `Name` → `Key`).
     - `StreamerMode { set; }` — main-thread setter; redact secrets in your status text when true. No-op if the source has nothing to redact.
     - `event StatusChanged` — raise when the status text changes (don't spam: not real-time). The menu and the in-game info panel re-fetch `GetStatusText` off this.
     - `GetStatusText(CancellationToken)` — what shows in the info panel. Honour the token for any I/O. Resolve any Polyglot strings here (or lazily), **not** in the ctor/field-init — localization isn't loaded when descriptors are constructed (see Localization).
     - `PreconditionMet()` — gates `GameplayHeartRateInstaller` from binding the source (token set, dependency installed, etc.).
     - `Dispose()` (only when implementing `IDisposableSourceDescriptor<T>`) — unsubscribe from any events you wired up. Not for `MonoBehaviour` descriptors; use `OnDestroy` there.
3. Add a config field in `PluginConfig` if needed.
4. Add a UI block to `UI/BSML/dataSource.bsml` and wire any custom controls in `DataSourceMenu.cs`. Use `text-key`/`~`-bound Polyglot keys and add the rows to `Localization.csv` (see Localization). The data-source dropdown and info panel pick up new registrations automatically — no menu changes needed for the dropdown entry or status display.

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

**Important convention**: when a `[UIValue]` property is backed by a config property, **name the BSML key identically to the config property name** and use `nameof(Config.X)` so that a `Config` rename produces a compile error instead of a silent UI break. The forwarded `NotifyPropertyChanged(args.PropertyName)` then lights up the BSML binding automatically with no per-property switch in the controller. The setter must guard with a value-equality check to avoid feedback loops when BSML writes back unchanged values.

Example:
```csharp
[UIValue(nameof(Config.StreamerMode))]
public bool StreamerMode
{
    get => Config.StreamerMode;
    set
    {
        if (Config.StreamerMode != value) Config.StreamerMode = value;
    }
}
```

Only override the per-property `OnConfigChanged(string propertyName)` hook in the subclass when the change requires extra work beyond a simple BSML notify (refreshing derived text, kicking off network calls, etc.).

For UI state that isn't config-backed (button enabled state, status text, modal text), use `[UIValue(nameof(SelfProp))]` properties and call `NotifyPropertyChanged()` from the setter. `nameof(SelfProp)` keeps the attribute and the `[CallerMemberName]` notify in sync on rename. The `field` keyword keeps the boilerplate minimal.

`[UIAction]` follows the same rule: use `nameof(MethodName)` when the BSML id matches the C# method name. Existing kebab-case BSML ids that don't match the method name (e.g. `"reset-low-color"`) are left as string literals.

## Localization

Strings live in `HRCounter/Resources/Localization.csv` (embedded resource) and are injected into the game's BGLib.Polyglot at load by `Patches/LocalizationPatch.cs` (Harmony prefix on `LocalizationAsyncInstaller.LoadResourcesBeforeInstall`). BGLib.Polyglot is Beat Saber's own modified fork of the Polyglot library, so its CSV schema and runtime API match upstream Polyglot.

**CSV format** (Polyglot's fixed 30-column schema — **keep the comma count identical to existing rows**):
- Field 1 = key, field 3 = English, field 20 = Simplified Chinese; all other fields empty. That's 29 commas per logical row.
- Multi-line values: put a real newline inside the quotes (see the multi-line rows already in the file). Embedded newlines don't add commas.
- Untranslatable values (brand names, IDs): leave the Chinese field **empty** — don't duplicate the English.
- Keys are `SCREAMING_SNAKE_CASE`, prefixed `HRCOUNTER_`. Menu strings use `HRCOUNTER_<MENU>_...`; per-source descriptor strings use `HRCOUNTER_<SOURCE>_SOURCE_DESCRIPTOR_...`. Descriptor keys are intentionally **separate** from menu keys even when the text is identical.

**Resolving in C#**: `Localization.Get("KEY")` (Polyglot returns the key itself if missing) or `Localization.Instance.GetFormatOrKey("KEY", arg0, ...)` for `{0}`-formatted strings. Prefer a single formatted entry over concatenating a label key with a separate value key.

**In BSML**: `text-key="KEY"` for static text, `~KEY` for bound values. Match a `[UIValue]` BSML key to the config property name per the UI convention above.

**Load timing (important)**: descriptors are constructed during game **app init**, *before* the localization tables are fully populated. Calling `Localization.Get` that early does **not** throw or return the raw key — SiraLocalizer (which loads other CSVs and registers the extra supported languages) makes it fall back to the **English** text regardless of the player's selected language. So an eagerly-resolved string gets frozen in English even for a Chinese user. Resolve lazily instead so the current language is used: in `GetStatusText`, or via a `Lazy<string>` field (see `OscDescriptor._text`). Passing Polyglot **keys** as strings (labelKey, `NameKey`) is fine — the consumer resolves them later.

## Memory file

A user-level memory note exists: **GameObjects don't need explicit `Object.Destroy` in `Dispose`** for in-game scene objects — Unity scene unload cleans up. Only flag missing destroys for objects that outlive the scene (DontDestroyOnLoad, app-scope singletons).

## Common pitfalls

- **`Plugin.Instance.UserAgent`** is the canonical UA for HTTP/WS clients. Always set `_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Plugin.Instance.UserAgent)`.
- **Abstract base classes that need a stable-name logger** must use the ID-based pattern above (declare `[Inject(Id = typeof(Base))] Logger` and add the matching `WithId` line in `AppInstaller`). Don't rely on `context.MemberInfo` to derive the declaring type — Zenject's `InjectContext` doesn't expose it.
- **Field initializers vs constructors**: when a base class needs `Plugin.Instance` data, prefer field initializer (`= Plugin.Instance.UserAgent`) over a constructor — keeps subclasses constructor-free.
- **`field` keyword** is used in BSML view controllers for auto-property setters that need to call `NotifyPropertyChanged()`. Requires the `LangVersion` set in `HRCounter.csproj`; don't downgrade it.
- **`ReadAsStringAsync`** does **not** accept a `CancellationToken` on .NET Framework 4.7.2. Don't try to pass one.
- **`init` accessors** are unavailable on net472. Use `set` with `private`/internal access if you want immutability.
- **`string.IsNullOrEmpty` / `IsNullOrWhiteSpace` don't null-narrow** — the game's Mono reference assemblies lack the `[NotNullWhen(false)]` annotation, so the compiler still warns "possible null" after the check (and `TreatWarningsAsErrors` is on). Force it with the null-forgiving operator: `if (!string.IsNullOrEmpty(s)) return f(s!);` (see `DataSourceUtils.GetDisplayName`).
- **`Object.Destroy(prefabInstance)`** must be called by whoever creates a transient prefab when the operation fails (e.g. `HRCounter.Setup()` destroys the canvas if `SetupCounter` returns false).

## Don't

- Don't write `README.md` or other docs unless explicitly requested.
- Don't introduce `init`-only properties (net472).
- Don't add `Object.Destroy` to `IDisposable.Dispose` for scene objects.
- Don't bypass git hooks or sign-off requirements.
- Don't try to auto-discover ID-based logger bindings via reflection; that was reverted. Add `WithId` lines manually when introducing a new base class.
- Don't add backwards-compatibility shims or "removed" comments — delete cleanly.
- Don't add speculative abstractions for hypothetical future sources/displays. Three similar lines beats a premature framework.
