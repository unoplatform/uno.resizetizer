# Quickstart: Theme-aware splash screens

For Uno app authors who want their splash screen to match the OS's light/dark theme on Android (API 31+), iOS (13+), and WebAssembly.

## 1. Declare a dark variant

Open your project's `.csproj` (or your shared `base.props` / `Directory.Build.props`) and extend the existing `UnoSplashScreen` item with the new dark metadata:

```xml
<ItemGroup>
  <UnoSplashScreen
      Include="Assets/SplashScreen/splash.svg"
      BackgroundColor="#FFFFFF"
      DarkBackgroundColor="#000000"
      DarkImage="Assets/SplashScreen/splash_dark.svg" />
</ItemGroup>
```

All three new attributes are optional and independent. Leave any out to fall back to the light value:

- `BackgroundColor` only — no dark override, single light splash (today's behavior, except the default for "no color at all" is now `#F3F3F3`).
- `BackgroundColor` + `DarkBackgroundColor` — dark mode uses the dark color, same image.
- `BackgroundColor` + `DarkImage` — dark mode uses the dark image, same background color.
- All three — full light/dark pair.

`Color` (the legacy attribute name) continues to work as a synonym for `BackgroundColor`. Don't declare both on the same item.

## 2. Build and run

```
dotnet build -f net8.0-android
dotnet build -f net8.0-ios
dotnet build -f net8.0-browserwasm
```

No theme snippets in `Styles.xml`, `Info.plist`, or your Wasm `index.html` need to change. The resource/manifest selection is driven entirely by what resizetizer emits.

## 3. Verify

### Android (API 31+)

Install the app, toggle **Settings → Display → Dark theme**, relaunch. The splash should flip color and image. Pre-API-31 devices always show the light splash — this is documented and expected.

### iOS (13+)

Launch on a simulator or device set to **Settings → Developer → Dark Appearance**. The storyboard picks the dark named-color and dark image automatically via the asset catalog's appearance variants.

### WebAssembly

Open the site in a browser with dark mode preferred (`prefers-color-scheme: dark` — most OS-level dark settings are surfaced this way). Requires a version of `Uno.Wasm.Bootstrap` that consumes `splashScreenImageDark` / `splashScreenColorDark`; older bootstraps will show the light splash.

## Verified acceptance path (maps to spec stories)

| Spec scenario | Steps to reproduce |
|---|---|
| US1 AC1 (Android dark) | Declare full triplet, API 31+ device in dark mode → dark color + `splash_dark.svg` |
| US1 AC2 (iOS dark)     | Declare full triplet, iOS 13+ simulator in dark mode → dark color + dark image |
| US1 AC3 (WASM dark)    | Declare full triplet, browser with `prefers-color-scheme: dark` + compatible bootstrap → dark color + dark image |
| US1 AC4 (light mode)   | Same triplet, OS in light → light color + light image on all platforms |
| US2 AC1 (legacy)       | Declare only `Color="#512BD4"` → `#512BD4` on all platforms both themes, unchanged vs. pre-upgrade |
| US3 AC1 (partial color)| Declare `BackgroundColor + DarkBackgroundColor`, no `DarkImage` → dark color + light image in dark mode |
| US3 AC2 (partial image)| Declare `BackgroundColor + DarkImage`, no `DarkBackgroundColor` → light color + dark image in dark mode |

## FAQ

**Q: Why doesn't my app's `RequestedTheme` affect the splash?**
The splash is rendered by the OS *before* your app runs. It tracks the OS system theme only. Use `BackgroundColor`/`DarkBackgroundColor` to control the visuals.

**Q: My Android device is on API 30 and still shows the light splash in dark mode — bug?**
Not a bug. Theme-aware splash is API 31+ only. Build the light variant attractively so it works in both themes on older devices.

**Q: Can I use a PNG for dark and SVG for light (or vice versa)?**
Yes. `DarkImage` accepts the same formats as `Include` and they can differ.

**Q: What happens on Windows / Skia desktop / macOS Catalyst?**
Dark metadata is silently ignored — those platforms are out of scope for this release and always use the light variant. No build warnings.
