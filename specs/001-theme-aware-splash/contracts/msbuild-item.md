# Contract: `UnoSplashScreen` MSBuild item (authoring surface)

**Audience**: consumers of `Uno.Resizetizer` — Uno app projects declaring a splash screen in their `.csproj`.

## Accepted metadata (after this feature)

```xml
<ItemGroup>
  <UnoSplashScreen
      Include="Assets/SplashScreen/splash.svg"
      BackgroundColor="#FFFFFF"
      DarkBackgroundColor="#000000"
      DarkImage="Assets/SplashScreen/splash_dark.svg"
      BaseSize="128,128" />
</ItemGroup>
```

| Metadata | Required | Supported formats | Notes |
|---|---|---|---|
| `Include` | yes | `.svg`, `.png` | Light / default image. Existing behavior. |
| `BackgroundColor` | no | any `SKColor`-parseable string | Replaces `Color`. Default `#F3F3F3` when omitted. |
| `Color` | no | any `SKColor`-parseable string | **Legacy alias** for `BackgroundColor`. Mutually exclusive with `BackgroundColor`. Continues to work indefinitely. |
| `DarkBackgroundColor` | no | any `SKColor`-parseable string | Dark-mode background. Default `#202020` only when light background also defaulted; else falls back to the light value. |
| `DarkImage` | no | `.svg`, `.png` (same pipeline as `Include`) | Dark-mode image. Falls back to `Include` when omitted. Mixed formats allowed (SVG + PNG). |
| `BaseSize`, `Scale`, `AndroidScale`, `IOSScale`, `WasmScale`, `ForegroundScale`, … | no | existing | Applied identically to light and dark rasters. |

## Build-time guarantees

1. Declaring only light metadata produces output structurally equivalent to pre-feature output (SC-002). `drawable-night-*`, `values-night/`, and `splashScreenImageDark`/`splashScreenColorDark` fields are NOT emitted in that case.
2. `DarkImage` uses the same resolution rules as `Include`.
3. Any supported platform target with dark metadata declared but no dark-splash support (Windows/WinAppSDK, Skia desktops, macOS Catalyst) silently ignores dark metadata and emits only the light splash (FR-015). Build does not warn or error.

## Errors

| Condition | Severity | Message sketch |
|---|---|---|
| `DarkImage` file does not exist | error (build fails) | `"UnoSplashScreen item '{item}' declares DarkImage='{path}' but the file does not exist."` |
| `BackgroundColor` value unparseable | error | `"UnoSplashScreen item '{item}' has an invalid BackgroundColor='{value}'."` |
| `DarkBackgroundColor` value unparseable | error | `"UnoSplashScreen item '{item}' has an invalid DarkBackgroundColor='{value}'."` |
| Both `BackgroundColor` and `Color` declared on the same item | error | `"UnoSplashScreen item '{item}' declares both Color and BackgroundColor; use only one (BackgroundColor is preferred)."` |

## Out of scope

- Platform-specific *dark* metadata variants (e.g. `AndroidDarkImage`) — not in this feature. A single `DarkImage` applies to every platform that supports dark splash.
- Layer-semantic dark names (e.g. `DarkForegroundImage`) — deferred to uno.resizetizer#333.
- Deprecation of `Color` — not in this feature (Assumption: `Color` remains supported indefinitely).
