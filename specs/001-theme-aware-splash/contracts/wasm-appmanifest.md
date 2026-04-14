# Contract: WASM `UnoAppManifest.js` output

**Audience**: `GenerateWasmSplashAssets_v0` implementers and the `Uno.Wasm.Bootstrap` runtime that consumes the manifest.

**Design note — reuse of existing bootstrap fields**: The bootstrap already defines `lightThemeBackgroundColor` and `darkThemeBackgroundColor` CSS-var driven fields, and its `uno-bootstrap.css` already has a `@media (prefers-color-scheme: dark)` rule on `.uno-loader` that swaps on `--dark-theme-bg-color`. This feature reuses those existing fields for color rather than introducing a new `splashScreenColorDark`. Only the image side genuinely needs a new field (`splashScreenImageDark`) because there is no existing CSS-variable-backed mechanism for the loader `<img>` element's `src`.

## Emitted shape

```javascript
var UnoAppManifest = {
    // existing fields merged through from the user's AppManifest.js:
    displayName: "…",
    // (plus any other author-declared fields the existing parser preserves)

    // Light splash image — existing field, unchanged:
    splashScreenImage: "splash.scale-200.png",

    // Color fields:
    //   - When NO dark metadata is declared: emit only splashScreenColor (byte-compat with pre-feature output).
    //   - When ANY dark metadata is declared (DarkBackgroundColor OR DarkImage): emit
    //     lightThemeBackgroundColor + darkThemeBackgroundColor (both always, with per-attribute
    //     fallback to the light value) and OMIT splashScreenColor entirely so the bootstrap's
    //     @media query drives theme selection.
    splashScreenColor: "#F3F3F3",                        // only when no dark metadata
    lightThemeBackgroundColor: "#F3F3F3",                // only when any dark metadata
    darkThemeBackgroundColor: "#202020",                 // only when any dark metadata

    // NEW dark image field — only when DarkImage is declared:
    splashScreenImageDark: "splash_dark.scale-200.png",
}
```

## Field specification

### `splashScreenImage` (existing)

- Type: string — bare filename of the rasterized light image, relative to the WASM output root.
- Value: produced by `ResizeImageInfo.OutputName` + scale suffix + extension.
- **Format-neutral**: callers MUST NOT assume `.png`. Today's output is `.png` per the SkiaSharp rasterization pipeline, but the extension is the source of truth; future SVG passthrough (uno.resizetizer#259) will change the extension without changing the field.

### `splashScreenColor` (existing — now emitted only in the no-dark case)

- Type: string — hex color without alpha, e.g. `"#512BD4"`.
- **Emission rule**: emitted iff neither `DarkBackgroundColor` nor `DarkImage` is declared. In that case the item reduces to today's single-theme splash and the field is preserved byte-identical to pre-feature output.
- Default when author declares nothing *and* no dark metadata: `"#F3F3F3"` (FR-011, still emitted directly).
- When any dark metadata is declared, `splashScreenColor` is OMITTED in favor of the per-theme pair below. (The bootstrap's legacy inline `splashScreenColor` path would otherwise clobber the `@media (prefers-color-scheme: dark)` rule.)

### `lightThemeBackgroundColor` (existing bootstrap field — NEW for resizetizer to emit)

- Type: string — hex color without alpha.
- **Emission rule**: emitted iff any dark metadata (`DarkBackgroundColor` OR `DarkImage`) is declared.
- Value: the declared `BackgroundColor`/`Color`, else `"#F3F3F3"` default (FR-011).
- Consumed by the bootstrap's `--light-theme-bg-color` CSS variable; applied to `.uno-loader` by existing CSS.

### `darkThemeBackgroundColor` (existing bootstrap field — NEW for resizetizer to emit)

- Type: string — hex color without alpha.
- **Emission rule**: emitted iff any dark metadata (`DarkBackgroundColor` OR `DarkImage`) is declared.
- Value: the declared `DarkBackgroundColor` if present; else the declared `BackgroundColor`/`Color` (per-attribute fallback, FR-005); else `"#202020"` (FR-011) when both light and dark are defaulted.
- Consumed by the bootstrap's `--dark-theme-bg-color` CSS variable; applied to `.uno-loader` by the existing `@media (prefers-color-scheme: dark)` rule.

### `splashScreenImageDark` (NEW)

- Type: string — bare filename, same conventions as `splashScreenImage`.
- **Emission rule**: emitted iff `ResizeImageInfo.DarkFilename` is non-null. When absent, the bootstrap treats the value as identical to `splashScreenImage` (fallback per FR-005).
- **Extension may differ from `splashScreenImage`**: mixed-format case is explicitly supported per spec clarification (SVG light + PNG dark, or vice versa).

## Wire-protocol stability

- Field **names** are frozen by this feature. A future SVG passthrough feature MUST NOT rename these fields.
- Field **values** are paths (for image fields) or hex colors (for color fields). No nested objects.
- Absent-key semantics: `splashScreenImageDark` absent ≡ "reuse `splashScreenImage`". Never emit empty string, `null`, or the light value to indicate absence.

## Dependency on `Uno.Wasm.Bootstrap`

`Uno.Wasm.Bootstrap` (coordinated change, landed on `dev/mazi/theme-aware-splash`) MUST:

1. Read `splashScreenImageDark` from `UnoAppManifest` and select between it and `splashScreenImage` via `window.matchMedia('(prefers-color-scheme: dark)')` at splash-render time (FR-012).
2. Treat absent `splashScreenImageDark` as "fall back to `splashScreenImage`".
3. Honor `lightThemeBackgroundColor` / `darkThemeBackgroundColor` on the `.uno-loader` element via the existing CSS-variable path (`--light-theme-bg-color` / `--dark-theme-bg-color` with the existing `@media (prefers-color-scheme: dark)` rule). Already implemented; only requires NOT applying `splashScreenColor` as an inline override when those per-theme values are present, otherwise the media query is clobbered.
4. NOT reinvent the `#F3F3F3` / `#202020` defaults — resizetizer always emits concrete values (FR-011).

Color-side wire contract requires no new bootstrap fields — existing `lightThemeBackgroundColor` / `darkThemeBackgroundColor` fields are reused. Only the image side (`splashScreenImageDark` + matchMedia selection) is a genuine new field + runtime behavior.

Older bootstrap versions that predate the matchMedia image switch simply ignore `splashScreenImageDark` and show `splashScreenImage` in both themes — visually-equivalent to a partial-declaration fallback, not a regression.

## Existing AppManifest field preservation

`GenerateWasmSplashAssets_v0` today reads the user's `AppManifest.js` and merges the splash fields in via `FindWhatINeed` → `WriteToFile`. This feature preserves that behavior verbatim — dark fields are appended to the same dictionary, and non-splash fields pass through untouched. The `FindWhatINeed` parser's current `pair.Split(':')` limitation (values may not contain `:`) remains; no spec field value introduced here contains a colon.
