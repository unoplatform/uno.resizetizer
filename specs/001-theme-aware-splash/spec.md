# Feature Specification: Theme-Aware Splash Screens

**Feature Branch**: `001-theme-aware-splash`
**Created**: 2026-04-14
**Status**: Draft
**Input**: User description: "Add proper support for theme-aware splash screens. This has previously worked on WASM via Uno.Wasm.Bootstrap, but is now likely broken due to the fact that resizetizer is handling it. Related: unoplatform/uno#8096, unoplatform/uno.resizetizer#346. Should be supported on Android, iOS and WASM."

## Clarifications

### Session 2026-04-14

- Q: WASM manifest contract — should dark/light image fields use format-neutral names so future SVG passthrough (uno.resizetizer#259) doesn't require a wire-protocol change? → A: Yes. Emit format-neutral field names (e.g. `splashScreenImage`, `splashScreenImageDark`); values are paths and the file extension implies format.
- Q: Metadata naming — use `DarkImage` today and let future layered/adaptive splash (uno.resizetizer#333) add layer-semantic names (e.g. `ForegroundImage`) additively, or rename up front? → A: Keep `Include` + `DarkImage`. Future layer-semantic metadata will be added additively; `Include` will alias the foreground layer at that time, with no breakage.
- Q: Android drawable output — structure dark variants so future VectorDrawable output (uno.resizetizer#258) can swap content without path churn? → A: Yes. Place dark variants in standard Android-qualified resource folders (`drawable-night-v31/`, `values-night/`) using identical filenames to the light variants. Content format (raster/vector) is independent of folder structure.
- Q: `DarkImage` accepted formats — same rules as `Include`, including mixed formats (e.g. SVG `Include` + PNG `DarkImage`)? → A: Yes. `DarkImage` accepts the same formats as `Include` via the same resolution pipeline; mixed formats across the pair are allowed.
- Q: When neither `BackgroundColor` nor the legacy `Color` is declared, what default(s) should the splash use per theme? → A: Use fixed Uno-aligned defaults: light = `#F3F3F3`, dark = `#202020`. Resizetizer emits these concrete values into the generated artifacts (including the WASM manifest) so behavior is identical across platforms and independent of bootstrap runtime resolution.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Declare a dark-mode splash variant alongside a light default (Priority: P1)

An Uno app author wants their splash screen to respect the end-user's system theme. They declare a single `UnoSplashScreen` item in their `.csproj` with a light-mode background color and image, plus optional `DarkBackgroundColor` and `DarkImage` metadata for the dark-mode variant. When the app is launched on a device with the OS in dark mode, the splash screen appears with the dark background and dark artwork; when launched in light mode, it appears with the light defaults. No code changes in the app are required — declaration is purely in project metadata.

**Why this priority**: This is the feature's core value proposition. Without it, nothing else matters. Solves the regression captured in unoplatform/uno.resizetizer#346 and restores behavior lost from unoplatform/uno#8096.

**Independent Test**: Add `BackgroundColor`, `DarkBackgroundColor`, and `DarkImage` to a sample app's `UnoSplashScreen` item. Build and run on Android, iOS, and WASM targets. Toggle the OS theme between light and dark and relaunch. The splash screen must visually match the declared variant for the active OS theme on each platform.

**Acceptance Scenarios**:

1. **Given** an app declares `<UnoSplashScreen Include="splash.svg" BackgroundColor="#FFFFFF" DarkBackgroundColor="#000000" DarkImage="splash_dark.svg" />`, **When** the app is launched on Android (API 31+) with the OS in dark mode, **Then** the splash screen renders with a `#000000` background and the `splash_dark.svg` artwork.
2. **Given** the same declaration, **When** the app is launched on iOS 13+ with the OS in dark mode, **Then** the splash storyboard renders with the dark color and dark image via the system's appearance traits.
3. **Given** the same declaration, **When** the app is launched on WASM in a browser with `prefers-color-scheme: dark`, **Then** the splash screen shown during bootstrap uses the dark color and dark image.
4. **Given** the same declaration, **When** any of the above platforms is in light mode, **Then** the splash screen renders with `#FFFFFF` background and `splash.svg` artwork.

---

### User Story 2 - Backward-compatible upgrade for existing apps (Priority: P1)

An Uno app author upgrades to a resizetizer version that supports theme-aware splash screens, but has not yet added any dark metadata. Their existing `UnoSplashScreen` item (using only `Color` or only a default image) continues to work exactly as before on all platforms, in both light and dark OS themes. No build breaks, no visual regressions.

**Why this priority**: Equal priority to Story 1. A feature that breaks existing apps on upgrade is a non-starter. This story is what makes the feature safe to ship.

**Independent Test**: Build and run a sample that uses only the legacy `Color` metadata (no `BackgroundColor`, no dark variants). On each platform, in both OS themes, verify the splash screen renders identically to the behavior before this feature was added.

**Acceptance Scenarios**:

1. **Given** an existing project using only `<UnoSplashScreen Include="splash.svg" Color="#512BD4" />`, **When** the project is built and run on any supported platform, **Then** the splash screen uses `#512BD4` as the background in both light and dark OS themes, and displays `splash.svg` as the image.
2. **Given** an existing project that has not declared any dark metadata, **When** the project is rebuilt after the resizetizer upgrade, **Then** the build succeeds without new warnings or errors related to splash screens.
3. **Given** the legacy `Color` attribute is used, **When** the build runs, **Then** `Color` is accepted as a synonym for `BackgroundColor` and behavior is unchanged.

---

### User Story 3 - Partial dark declarations degrade gracefully (Priority: P2)

An app author declares only a `DarkBackgroundColor` but no `DarkImage` (or vice versa). The splash screen in dark mode uses the declared dark override for the specified field and falls back to the light (default) value for the field that was not overridden.

**Why this priority**: Authors commonly need only a different background color, not different artwork. Forcing all-or-nothing would be an unnecessary barrier.

**Independent Test**: Declare only `DarkBackgroundColor` (no `DarkImage`). Verify on each platform that in dark mode the background color changes but the image remains the same as the light variant. Repeat inversely with only `DarkImage` declared.

**Acceptance Scenarios**:

1. **Given** `<UnoSplashScreen Include="splash.svg" BackgroundColor="#FFF" DarkBackgroundColor="#000" />` with no `DarkImage`, **When** run in dark mode on any supported platform, **Then** the splash shows `#000` background with the light `splash.svg` image.
2. **Given** `<UnoSplashScreen Include="splash.svg" BackgroundColor="#FFF" DarkImage="splash_dark.svg" />` with no `DarkBackgroundColor`, **When** run in dark mode on any supported platform, **Then** the splash shows `#FFF` background with `splash_dark.svg`.

---

### Edge Cases

- **Transparent or unspecified background color**: When `BackgroundColor` or `DarkBackgroundColor` is transparent (or unspecified), the splash uses fixed Uno-aligned defaults — `#F3F3F3` (light) and `#202020` (dark) — applied identically on all supported platforms.
- **Android below API 31**: Dark variant is not honored on pre-31 devices (modern splash-screen API is unavailable); the light/default splash is used regardless of system theme. Documentation must state this explicitly.
- **User toggles OS theme mid-launch**: The splash reflects the theme active at the moment the OS renders it; a mid-launch toggle does not cause a re-render during the already-visible splash.
- **App overrides Uno's `RequestedTheme` at runtime**: Splash screens are rendered by the OS before Uno app code executes, so runtime theme overrides do **not** affect the splash. Splash always follows the OS system theme.
- **Dark image references a file that does not exist**: Build fails with a clear error message pointing at the `DarkImage` metadata on the offending `UnoSplashScreen` item.
- **Legacy `Color` declared alongside new `BackgroundColor`**: Build fails with a clear error requiring the author to choose one. (They are aliases; declaring both is ambiguous.)
- **DarkImage declared but no BackgroundColor or Color at all**: Build uses platform-appropriate defaults (transparent or white) for both themes' backgrounds, same as today's no-color behavior.

## Requirements *(mandatory)*

### Functional Requirements

**Authoring model**

- **FR-001**: The `UnoSplashScreen` MSBuild item MUST accept a new `BackgroundColor` metadata attribute as the replacement name for the existing `Color` attribute.
- **FR-002**: The `UnoSplashScreen` MSBuild item MUST accept `Color` as a fully supported alias for `BackgroundColor`, preserving existing project compatibility. Declaring both `Color` and `BackgroundColor` on the same item MUST produce a clear build error.
- **FR-003**: The `UnoSplashScreen` MSBuild item MUST accept an optional `DarkBackgroundColor` metadata attribute specifying the background color used when the OS system theme is dark.
- **FR-004**: The `UnoSplashScreen` MSBuild item MUST accept an optional `DarkImage` metadata attribute specifying an alternate image asset used when the OS system theme is dark. The value MUST point to an image file the project can resolve (relative path or project-rooted path), using the same resolution rules as the default image attribute.
- **FR-005**: All dark-variant metadata attributes MUST be optional. When omitted, the dark mode rendering MUST fall back to the corresponding light (default) value on a per-attribute basis (partial overrides supported).

**Android behavior**

- **FR-006**: On Android API 31+, the build MUST generate theme-aware splash resources such that the system splash screen uses the dark background color and dark image when the OS is in dark mode, and the light values otherwise. Dark variants MUST be emitted into standard Android-qualified resource folders (`drawable-night-v31/`, `values-night/`) using the same filenames as their light counterparts, so the OS performs selection via the `UI_MODE_NIGHT` qualifier. Filenames MUST be independent of asset content format, preserving forward compatibility with a future switch from raster to VectorDrawable output (uno.resizetizer#258).
- **FR-007**: On Android API levels below 31, the build MUST generate only the light/default splash resources and the splash MUST behave identically in both light and dark OS themes (no dark variant applied). This limitation MUST be documented.

**iOS behavior**

- **FR-008**: On iOS 13+, the build MUST generate a single splash storyboard that references color and image assets resolvable per appearance trait (light/dark), so that the OS renders the correct variant automatically without requiring a second storyboard file.
- **FR-009**: When the dark metadata is absent, the generated storyboard MUST not declare dark-variant assets, preserving pre-feature output byte-for-byte where practical.

**WASM behavior**

- **FR-010**: On WASM, the build MUST emit both light and dark splash configuration (background color and image filename, independently) into the artifacts consumed by `Uno.Wasm.Bootstrap` (today: `AppManifest.js`). Image-path fields MUST use format-neutral names (e.g. `splashScreenImage`, `splashScreenImageDark`) whose value is a bare path; the file extension (`.png`, `.svg`, etc.) implies the format. This preserves wire-protocol compatibility with future SVG passthrough (uno.resizetizer#259). Color fields MUST reuse the existing bootstrap manifest fields `lightThemeBackgroundColor` / `darkThemeBackgroundColor` (already consumed by the bootstrap's `--light-theme-bg-color` / `--dark-theme-bg-color` CSS variables under its `@media (prefers-color-scheme: dark)` rule) rather than introducing new names. When any dark metadata is declared, `splashScreenColor` MUST NOT be emitted (it would clobber the bootstrap's media-query-driven theme switch via its inline style); when no dark metadata is declared, `splashScreenColor` is emitted unchanged for byte-compat with pre-feature output.
- **FR-011**: When `BackgroundColor` (or its alias `Color`) is unspecified or transparent, the splash MUST use a fixed Uno-aligned light-theme default of `#F3F3F3`. When `DarkBackgroundColor` is unspecified or transparent, the dark splash MUST use a fixed dark-theme default of `#202020`. These defaults apply uniformly across Android, iOS, and WASM. On WASM, resizetizer MUST emit these concrete hex values into the manifest (into `lightThemeBackgroundColor` / `darkThemeBackgroundColor` when any dark metadata is declared, or into `splashScreenColor` otherwise) rather than relying on runtime resolution by the bootstrap.
- **FR-012**: The WASM dark-mode detection itself MUST be performed by `Uno.Wasm.Bootstrap` at runtime via the browser's `prefers-color-scheme` media query; resizetizer's responsibility is limited to emitting the two variants into the manifest.

**Error handling & diagnostics**

- **FR-013**: If `DarkImage` references a file that cannot be found, the build MUST fail with an error message naming the offending `UnoSplashScreen` item and the missing file path.
- **FR-014**: If any color metadata (`BackgroundColor`, `DarkBackgroundColor`, or legacy `Color`) has an invalid color value, the build MUST fail with a clear error that names the attribute and the item.

**Out of scope (explicitly deferred to a follow-up)**

- **FR-015**: Theme-aware splash on Windows (UWP/WinAppSDK), Skia desktop targets (GTK/X11/Framebuffer/macOS/Windows-Skia), and macOS Catalyst is NOT required by this feature and MAY be added in a later release. The build MUST NOT error when dark metadata is declared but a target platform lacks dark-splash support; those platforms MUST silently fall back to the light/default splash.

**Documentation**

- **FR-016**: The `using-uno-resizetizer.md` and `uno-resizetizer-properties.md` documentation MUST be updated to describe `BackgroundColor`, `DarkBackgroundColor`, and `DarkImage`, including the Android API 31+ caveat and the clarification that splash always follows the OS (not the app's `RequestedTheme`).

### Key Entities

- **`UnoSplashScreen` item (authoring surface)**: An MSBuild item group entry declared in a `.csproj`. Carries metadata: default image path (Include), `BackgroundColor` (or alias `Color`), optional `DarkBackgroundColor`, optional `DarkImage`, plus existing scale/platform-scale metadata unchanged.
- **Splash generation artifacts (build output)**: Per-platform build-produced resources — Android drawables/color resources with night-qualifier counterparts, iOS storyboard with appearance-trait-aware references, WASM manifest entries for both light and dark color + image.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On each of Android (API 31+), iOS (13+), and WASM, an app that declares a full set of light + dark splash metadata shows the correct theme-matching splash in 100% of launches when the OS theme is set accordingly, verified by manual run on at least one physical or simulated device per platform.
- **SC-002**: An existing project that builds successfully on the current resizetizer release, and which has not added any dark metadata, continues to build successfully and produce visually identical splash output after upgrading — zero regressions in generated manifests, drawables, or storyboards (byte-level diff where feasible; otherwise structural equivalence).
- **SC-003**: Automated tests validate generated output for all three platforms: given a known input item with both light and dark metadata, the tests assert that the generated Android drawable/values-night XML, iOS storyboard, and WASM manifest contain the expected light and dark values; given input with only light metadata, the tests assert the output matches pre-feature output.
- **SC-004**: A user can enable theme-aware splash screens on all three platforms by editing only their `.csproj` — no code changes, no additional files beyond the dark image asset itself, no changes to `wwwroot/`, `Info.plist`, or Android resource folders by hand.
- **SC-005**: Published documentation clearly answers: "how do I make my splash dark-mode aware?", "why does my pre-API-31 Android device show the light splash?", and "why doesn't my app's forced dark theme apply to the splash?" — verified by reviewing the updated docs against these three questions.

## Assumptions

- The legacy `Color` attribute name remains supported indefinitely as an alias for `BackgroundColor`; this feature does not deprecate or remove it. Any deprecation would be a separate decision.
- `Uno.Wasm.Bootstrap` will be extended (in a coordinated change tracked separately) to consume the dark fields emitted into `AppManifest.js` and to apply `prefers-color-scheme` at runtime. This spec covers resizetizer's side of the contract; the bootstrap-side change is out of scope here but is a required dependency for WASM to function end-to-end.
- "System theme" means the OS-reported theme at splash-render time. Apps that force dark/light mode at runtime inside the Uno app lifecycle do not affect the splash, as the splash is rendered by the OS before app code runs.
- Splash screens are rendered briefly at launch; we assume the user does not toggle the OS theme during the splash display itself. No live theme-switch handling is required during an active splash.
- Android's pre-31 limitation (no dark variant) is acceptable for this release; the majority of supported devices are on API 31+ when the feature ships.
- The sample / test apps already present in the repository are sufficient scaffolding to host integration verification; no new sample app is required by this feature.

## Dependencies

- **Uno.Wasm.Bootstrap**: Requires a compatible version that reads dark splash fields from `AppManifest.js` and applies `prefers-color-scheme` at runtime. Initial release must be coordinated with a Wasm.Bootstrap release that supports these fields.
- **Existing resizetizer image-resolution pipeline**: `DarkImage` metadata uses the same image resolution and SkiaSharp rasterization path as the default image; no new image pipeline is introduced.
