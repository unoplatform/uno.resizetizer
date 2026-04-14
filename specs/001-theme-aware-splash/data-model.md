# Phase 1 Data Model: Theme-Aware Splash Screens

This is a build-time feature with no persistent storage. The "data model" here describes the authoring surface (MSBuild item metadata), the shape of the internal representation after parsing, and the generated-artifact shapes per platform.

## 1. Authoring entity — `UnoSplashScreen` item

MSBuild item group entry in a consumer `.csproj`. Single item per app (existing constraint).

| Metadata | Type | Required | Default | Validation |
|---|---|---|---|---|
| `Include` | path (file) | yes | — | Must resolve to an existing file. Format: SVG or PNG (existing rules). |
| `BackgroundColor` | color string | no | `#F3F3F3` (see FR-011) | Parseable by `Utils.ParseColorString`. Mutually exclusive with `Color`. |
| `Color` | color string (legacy alias) | no | — | As `BackgroundColor`. Declaring both `Color` and `BackgroundColor` on the same item → build error. |
| `DarkBackgroundColor` | color string | no | `#202020` *(applied only when the light `BackgroundColor`/`Color` has also defaulted, else falls back to the light value)* | Parseable by `Utils.ParseColorString`. |
| `DarkImage` | path (file) | no | falls back to `Include` | Must resolve to an existing file. Same format rules as `Include`. Mixed formats allowed (e.g. SVG `Include` + PNG `DarkImage`). |
| `BaseSize` | size string (existing) | no | — | Existing rules. |
| `Scale` / `AndroidScale` / `IOSScale` / `WasmScale` (existing) | number | no | — | Existing rules. Applied identically to light and dark rasters. |

**Fallback rule (FR-005)**: partial declarations fall back per attribute, not per item:

| Declared? | `BackgroundColor` result | `DarkBackgroundColor` result |
|---|---|---|
| none | `#F3F3F3` | `#202020` |
| only `BackgroundColor=X` | `X` | `X` (falls back to light) |
| only `DarkBackgroundColor=Y` | `#F3F3F3` | `Y` |
| both | `X` | `Y` |

| Declared? | Light image | Dark image |
|---|---|---|
| only `Include=L` | `L` | `L` (falls back) |
| `Include=L` + `DarkImage=D` | `L` | `D` |

## 2. Internal entity — `ResizeImageInfo` (extended)

Existing class at `src/Resizetizer/src/ResizeImageInfo.cs`. Additions for this feature:

```csharp
public string? DarkFilename { get; set; }      // resolved full path; null means no dark image override (fall back to Filename)
public SKColor? DarkColor { get; set; }        // parsed dark background; null means fall back to Color
public bool DarkIsVector => IsVectorFilename(DarkFilename);
public bool HasDarkOverride => DarkFilename != null || DarkColor != null;
```

`Parse(ITaskItem)` responsibilities (added):

1. Read `BackgroundColor` metadata; if empty, read `Color`. If both are non-empty → `LogError` and return null (or throw via existing path).
2. If parsed-non-null, set `Color`; else apply `#F3F3F3` default.
3. Read `DarkBackgroundColor`; if parsed-non-null, set `DarkColor`; else if `Color` was defaulted (step 2 applied `#F3F3F3`), set `DarkColor = #202020`; else set `DarkColor = Color` (per-attribute fallback).
4. Read `DarkImage`; if non-empty, resolve to full path and validate existence (throw `FileNotFoundException` with a message naming the `UnoSplashScreen` item if not found); else set `DarkFilename = null`.

State invariants after `Parse`: for any `IsSplashScreen = true` item, `Color` and `DarkColor` are both non-null `SKColor` values; `Filename` is always non-null; `DarkFilename` may be null (meaning "reuse `Filename`").

## 3. Generated artifact — Android

Per consumer project, emitted under `$(_UnoIntermediateSplashScreen)`:

```text
values/uno_colors.xml                                 # <color name="uno_splash_color"> = Color hex
drawable/uno_splash_image.xml                         # existing layer-list, references @color/uno_splash_color + @drawable/{outputName}
drawable-v31/uno_splash_image.xml                     # existing layer-list with explicit sized bitmap
drawable-*dpi/{outputName}.png                        # existing, rasterized light image per DPI

# NEW — only when HasDarkOverride:
values-night/uno_colors.xml                           # <color name="uno_splash_color"> = DarkColor hex
drawable-night-v31/uno_splash_image.xml               # layer-list identical in shape to drawable-v31, references @drawable/{outputName}
drawable-night-*dpi/{outputName}.png                  # rasterized dark image per DPI (from DarkFilename; falls back to light raster if DarkFilename is null but DarkColor differs)
```

Identical filenames across light/dark folders — Android's qualifier resolver selects by `UI_MODE_NIGHT`.

Note: when only `DarkColor` is declared (no `DarkImage`), we still emit `values-night/uno_colors.xml`, but we do NOT emit `drawable-night-*dpi/{outputName}.png`. The night-v31 layer-list still references `@drawable/{outputName}` — Android resolves that to the light raster, which is the intended fallback.

Note: when only `DarkImage` is declared (no `DarkColor`), we emit `drawable-night-*dpi/{outputName}.png` and `drawable-night-v31/uno_splash_image.xml`, but we do NOT emit `values-night/uno_colors.xml` — the night-v31 layer-list's `@color/uno_splash_color` resolves to the light color, which is the intended fallback.

## 4. Generated artifact — iOS

Per consumer project, emitted under `$(_UnoIntermediateSplashScreen)`:

```text
UnoSplash.storyboard                                  # existing template; now references namedColor "UnoSplashBackground" and named image "UnoSplashImage"
UnoInfo.plist                                         # existing partial plist pointing at UnoSplash storyboard

# NEW — xcassets entries consumed via the existing ProcessResizedImagesApple pipeline:
Assets.xcassets/UnoSplashBackground.colorset/Contents.json
Assets.xcassets/UnoSplashImage.imageset/Contents.json
Assets.xcassets/UnoSplashImage.imageset/{outputName}@1x.png
Assets.xcassets/UnoSplashImage.imageset/{outputName}@2x.png
Assets.xcassets/UnoSplashImage.imageset/{outputName}@3x.png
# + parallel dark entries inside the same .imageset/.colorset when HasDarkOverride,
#   carrying `"appearances":[{"appearance":"luminosity","value":"dark"}]`
```

`Contents.json` for `UnoSplashBackground.colorset`:

```json
{
  "info": { "author": "uno.resizetizer", "version": 1 },
  "colors": [
    { "color": { "color-space": "srgb", "components": { "red": "…", "green": "…", "blue": "…", "alpha": "…" } }, "idiom": "universal" },
    { "appearances": [{ "appearance": "luminosity", "value": "dark" }],
      "color": { "color-space": "srgb", "components": { "red": "…", "green": "…", "blue": "…", "alpha": "…" } },
      "idiom": "universal" }
  ]
}
```

When no dark metadata is declared, the second entry is omitted (FR-009).

## 5. Generated artifact — WASM (`UnoAppManifest.js`)

Two emission modes, selected by whether any dark metadata is declared:

**Mode A — no dark metadata (byte-compat with pre-feature output):**

```javascript
var UnoAppManifest = {
    splashScreenImage: "splash.scale-200.png",
    splashScreenColor: "#F3F3F3",
    // ... other existing AppManifest fields merged through from the user file
}
```

**Mode B — any dark metadata (`DarkBackgroundColor` or `DarkImage`) declared:**

```javascript
var UnoAppManifest = {
    splashScreenImage: "splash.scale-200.png",
    splashScreenImageDark: "splash_dark.scale-200.png",   // NEW — omitted if DarkFilename is null
    lightThemeBackgroundColor: "#F3F3F3",                 // existing bootstrap field, now emitted by resizetizer
    darkThemeBackgroundColor: "#202020",                  // existing bootstrap field, now emitted by resizetizer
    // splashScreenColor is OMITTED in this mode (see below)
    // ... other existing AppManifest fields merged through from the user file
}
```

Field-naming rules:
- `splashScreenImage*` values are **paths**. The file extension (`.png`, `.svg`, …) implies format. Wire protocol is format-neutral (per spec clarification, forward-compatible with uno.resizetizer#259).
- Color fields are hex strings without alpha (the existing `Utils.SkiaColorWithoutAlpha` helper is reused).
- Absent key (`splashScreenImageDark` missing entirely) is the signal to the bootstrap to reuse `splashScreenImage`. Empty-string or `null` values are NOT used.
- `splashScreenColor` is mutually exclusive with `lightThemeBackgroundColor`/`darkThemeBackgroundColor`: the former sets `background-color` inline (overriding any `@media`-driven theme switch), while the latter two drive the existing CSS-variable + media-query path. Emitting both would clobber theme switching.

## 6. State transitions

No runtime state. The build-time transition is:

```
UnoSplashScreen item  ──Parse──▶  ResizeImageInfo (with Dark* fields populated / defaulted)
                                        │
                                        ├──▶ GenerateSplashAndroidResources_v0 ──▶ values/, values-night/, drawable*, drawable-night-v31/
                                        ├──▶ ResizetizeImages_v0 (Android)      ──▶ drawable-*dpi/*.png, drawable-night-*dpi/*.png
                                        ├──▶ GenerateSplashStoryboard_v0        ──▶ UnoSplash.storyboard + xcassets
                                        ├──▶ ResizetizeImages_v0 (iOS)          ──▶ xcassets imageset PNGs (light + dark appearances)
                                        └──▶ GenerateWasmSplashAssets_v0        ──▶ UnoAppManifest.js
```

All tasks are idempotent and guarded by the existing `_UnoSplashStampFile` / `_UnoManifestStampFile` incremental stamps. Adding `Dark*` metadata to the item is part of the stamp's input set (via MSBuild's normal dependency tracking on the item's metadata), so changing only `DarkBackgroundColor` correctly invalidates the stamp.
