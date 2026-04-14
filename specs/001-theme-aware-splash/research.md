# Phase 0 Research: Theme-Aware Splash Screens

Spec: `specs/001-theme-aware-splash/spec.md` · Plan: `specs/001-theme-aware-splash/plan.md`

The spec's clarifications session already pinned every public contract choice (authoring names, WASM wire format, Android folder layout, defaults). This document resolves the remaining *how*-level unknowns — the platform mechanics of producing per-theme splash output — and records the alternatives considered.

---

## 1. Android: how does API 31+ honour a night-qualified splash?

**Decision**: Emit both light and dark variants under identical filenames into Android's standard qualified resource folders:

| Resource | Light path | Dark path |
|---|---|---|
| Background color resource (`@color/uno_splash_color`) | `values/uno_colors.xml` | `values-night/uno_colors.xml` |
| Layered drawable (`@drawable/uno_splash_image`) — pre-31 path | `drawable/uno_splash_image.xml` | `drawable-night/uno_splash_image.xml` *(only if pre-31 dark is ever wanted; we do NOT emit this today — see below)* |
| Layered drawable v31 (`@drawable/uno_splash_image` with explicit size) | `drawable-v31/uno_splash_image.xml` | `drawable-night-v31/uno_splash_image.xml` |
| Rasterized splash bitmap (referenced by the drawable `<bitmap android:src="@drawable/{outputName}"/>`) | `drawable-*dpi/{outputName}.png` | `drawable-night-*dpi/{outputName}.png` *(same filename, night qualifier)* |

The Android resource system auto-selects the `-night` variant when `UI_MODE_NIGHT_YES` is active. The app's theme references `@color/uno_splash_color` and `@drawable/uno_splash_image` unchanged (the same author-facing Styles.xml snippet already documented in `using-uno-resizetizer.md`); resolution is invisible to the theme.

**Pre-API-31 scope**: FR-007 restricts dark support to API 31+. We deliberately do NOT emit `drawable-night/uno_splash_image.xml` (the pre-31 layer-list). Reason: the pre-31 splash on Android is rendered by the legacy `windowBackground` path which gives the OS no theme-re-resolution opportunity between declaring the theme and rendering the activity window; results across OEM skins are inconsistent. Emitting only the v31 night variant gives us predictable behavior on the supported matrix and leaves the pre-31 path byte-identical to today.

**Rationale**: Android's qualifier-based resource resolution is the canonical mechanism; filename-identical siblings ensure a future VectorDrawable swap (uno.resizetizer#258) is purely a content-format change per folder, not a layout refactor.

**Alternatives considered**:
- *Runtime theme detection in app code* — rejected: would require new runtime code in every consuming Uno app, contradicting FR-SC-004 ("no code changes").
- *Two distinct drawable names (`uno_splash_image_light` / `_dark`) chosen by theme overlays* — rejected: doubles the author-facing theme snippet in `Styles.xml` and breaks FR-SC-004.
- *Single adaptive drawable with inline day/night references (`<day>/<night>` in a single XML)* — not supported for `layer-list`; only `color` has `day`/`night` attributes and even then it's not portable across API 31+ splash screen theming.

---

## 2. iOS: how does a single storyboard render light- and dark-mode variants?

**Decision**: Keep a single `UnoSplash.storyboard` (unchanged template) that references **named assets via `Assets.xcassets`**:

- The background color becomes a **named color asset** (e.g. `UnoSplashBackground`) in `Assets.xcassets/UnoSplashBackground.colorset/Contents.json` with two color entries: `appearances: []` (light/default) and `appearances: [{ appearance: "luminosity", value: "dark" }]` (dark).
- The image becomes a **named image asset** (e.g. `UnoSplashImage`) in `Assets.xcassets/UnoSplashImage.imageset/Contents.json` with per-DPI image entries that carry the `appearances` key identically (the same per-DPI raster set is duplicated once for the dark image).

The storyboard's `<color ... colorSpace="custom" customColorSpace="sRGB"/>` element is replaced with a `<namedColor name="UnoSplashBackground"/>` reference, and the `<imageView image="..."/>` refers to the named image asset. iOS renders the asset catalog's appearance-matched variant at launch without a second storyboard.

**Rationale**: One storyboard, one `Info.plist` entry, correct visuals — the Apple-recommended path for launch screens since iOS 13. Matches FR-008 exactly and keeps the partial `UnoInfo.plist` emitted today unchanged.

**Alternatives considered**:
- *Two storyboards selected by Info.plist keys* — iOS has no splash-screen equivalent of `UILaunchStoryboardName~dark`; rejected as unsupported.
- *Inline color literal + rely on `UIViewController.overrideUserInterfaceStyle`* — splash screens run before any view controller is alive; rejected.
- *Vector PDF asset with inline dark override* — adds a SkiaSharp→PDF pipeline we don't have; out of scope for this feature.

**When dark metadata is absent (FR-009)**: the `colorset` and `imageset` emit **only** the light entry (no `appearances` key), and the storyboard references them identically. Byte-for-byte diff with today's output may not be literally achievable because we are moving from a literal `<color>` element to a `<namedColor>` reference; instead SC-002's "structural equivalence" clause applies, and we add a targeted test asserting the no-dark case renders the same pixels on a reference simulator run.

---

## 3. WASM: what does `Uno.Wasm.Bootstrap` consume from `UnoAppManifest.js`?

**Decision**: Reuse existing bootstrap fields for color; add one new field for image.

Inspecting `src/Uno.Wasm.Bootstrap/ts/Uno/WebAssembly/Bootstrapper.ts` and `src/Uno.Wasm.Bootstrap/WasmCSS/uno-bootstrap.css` in the bootstrap repo revealed that:

- `lightThemeBackgroundColor` / `darkThemeBackgroundColor` already exist as manifest fields and are wired to CSS variables `--light-theme-bg-color` / `--dark-theme-bg-color` on `.uno-loader`, selected by an existing `@media (prefers-color-scheme: dark)` rule.
- No existing field backs the loader's `<img src>` for dark mode — this is a genuine gap and requires a new field plus JS-side `matchMedia` selection.

| Field | Today | After this feature |
|---|---|---|
| `splashScreenImage` | bare filename string (e.g. `"splash.scale-200.png"`) — kept, format-neutral | kept |
| `splashScreenColor` | hex string without alpha (e.g. `"#512BD4"`) — kept | emitted ONLY when no dark metadata is declared (byte-compat); omitted when any dark metadata is declared |
| `lightThemeBackgroundColor` | existing bootstrap field, not previously emitted by resizetizer | emitted when any dark metadata is declared |
| `darkThemeBackgroundColor` | existing bootstrap field, not previously emitted by resizetizer | emitted when any dark metadata is declared |
| `splashScreenImageDark` | *(absent)* | bare filename of the dark raster (format-neutral); emitted when `DarkImage` is declared |

When `DarkImage` is unset, `splashScreenImageDark` is omitted entirely (absent key, not empty string) and `Uno.Wasm.Bootstrap` treats absence as "reuse `splashScreenImage`".

**Why this split (color reuses existing fields, image adds a new one)**:
- Color-side: adding `splashScreenColorDark` would duplicate the existing `darkThemeBackgroundColor` functionality. The bootstrap already has the CSS wiring; emitting the existing fields means zero new wire contract for colors and zero new CSS.
- Image-side: there is no existing CSS-variable-backed mechanism for the `<img>` element's `src`, so a new field is unavoidable. Keeping the `Dark` suffix (matching `splashScreenImage`) is the only naming decision.

**Why `splashScreenColor` is suppressed when dark metadata exists**: the bootstrap's legacy `splashScreenColor` path sets `background-color` inline on `.uno-loader`, which would override the `@media`-driven CSS var switch. Emitting only the per-theme pair in that case avoids the clobber. (The companion bootstrap change also adds a defensive guard that ignores inline `splashScreenColor` when per-theme fields are present.)

**Alternatives considered**:
- *Add both `splashScreenColorDark` and `lightThemeBackgroundColor`/`darkThemeBackgroundColor`* — rejected as duplicative.
- *Add `splashScreenColorDark` only, ignore existing fields* — rejected: needless new wire contract where one already exists and works.
- *Nested object `splashScreen: { image, color, dark: { image, color } }`* — breaks wire-protocol compatibility with flat shape every deployed bootstrap reads. Rejected.
- *Let bootstrap resolve defaults at runtime* — FR-011 requires concrete emission. Rejected.

**Coordinated `Uno.Wasm.Bootstrap` change** (landed on branch `dev/mazi/theme-aware-splash` in `D:\Work\Uno.Wasm.Bootstrap`): bootstrap now selects between `splashScreenImage` and `splashScreenImageDark` via `matchMedia`, and its inline `splashScreenColor` setter yields to per-theme CSS vars when both are present. Initial release of the resizetizer feature must be coordinated with a bootstrap release containing these changes. Older bootstrap versions that predate the `matchMedia` image switch ignore `splashScreenImageDark` and show `splashScreenImage` in both themes — visually-equivalent to a partial-declaration fallback, not a regression.

---

## 4. SkiaSharp rasterization for the dark image

**Decision**: Reuse the existing `ResizetizeImages_v0` / `SkiaSharpSvgTools` / `SkiaSharpBitmapTools` pipeline unchanged. The `DarkImage` metadata is resolved by the same path-resolution rules as `Include` (FR-004), fed into the same tools, and produces a parallel set of per-DPI PNGs whose output filenames are the same as the light image's but routed into the `-night`-qualified Android folders and the dark-appearance slots of the iOS `.xcassets`.

**Rationale**: Zero new image-processing code; full format parity with `Include` (including the already-agreed mixed-format case, e.g. SVG light + PNG dark).

**Alternatives considered**:
- *Synthesize dark image from light by color inversion* — rejected: unpredictable results, violates authoring intent.
- *Require SVG-only `DarkImage`* — rejected by spec clarification ("DarkImage accepts the same formats as Include").

---

## 5. Error surface

**Decision**: Four build-time errors, emitted via `Log.LogError` from the relevant task (wrapped in `ResizeImageInfo.Parse` or the Android/iOS/WASM generators):

1. `DarkImage` references a file not on disk → file-not-found error naming the item and path (FR-013) — same class of error the existing `Include` path already throws via `FileNotFoundException` in `ResizeImageInfo.Parse`.
2. `DarkBackgroundColor` is an unparseable color → invalid-color error naming the attribute and item (FR-014) — mirrors the existing `Color` parsing path.
3. `BackgroundColor` and legacy `Color` both declared on the same item → "ambiguous; choose one" error (Edge Case / FR-002).
4. `BackgroundColor` is an unparseable color → same class as (2) (FR-014).

**Rationale**: Consistency with the existing image-resolution and color-parsing error pipeline in `ResizeImageInfo.Parse`. No new exception types are introduced.

**Alternatives considered**:
- *Warn and fall back to the light variant* for missing `DarkImage` — rejected: silently swallowing a declared-but-missing asset is a classic footgun, and the spec (FR-013) explicitly requires a hard error.

---

## 6. Defaults injection (`#F3F3F3` / `#202020`)

**Decision**: Apply defaults in `ResizeImageInfo.Parse` (or an immediately adjacent helper) so that by the time any generator sees the info object, `Color` and `DarkColor` are always non-null concrete values when the output should have a background. Generators never second-guess defaults.

**Rationale**: Single source of truth for the default rule; prevents divergence between Android, iOS, and WASM generators; satisfies FR-011's "emit concrete values ... rather than relying on runtime resolution."

**Alternatives considered**:
- *Inject defaults per-generator* — three copies of the same rule, risk of drift.
- *Inject only on WASM (where FR-011 explicitly names the manifest)* — would mean Android and iOS could end up transparent where WASM shows `#F3F3F3`, violating the spec's "applied identically on all supported platforms."

---

## Summary of unresolved items

None. All `NEEDS CLARIFICATION` are resolved via the spec's clarifications session and the decisions above. Proceeding to Phase 1.
