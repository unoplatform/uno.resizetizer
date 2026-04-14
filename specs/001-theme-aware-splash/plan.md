# Implementation Plan: Theme-Aware Splash Screens

**Branch**: `001-theme-aware-splash` | **Date**: 2026-04-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/001-theme-aware-splash/spec.md`

## Summary

Restore and extend theme-aware splash screen support across Android (API 31+), iOS (13+), and WASM by teaching Uno.Resizetizer's MSBuild tasks to consume new `BackgroundColor` / `DarkBackgroundColor` / `DarkImage` metadata on `UnoSplashScreen` items, and to emit per-theme variants into the correct platform output slots: Android qualified resource folders (`drawable-night-v31/`, `values-night/`), an iOS launch storyboard whose image and background reference appearance-trait-aware asset catalog entries, and — on WASM — a new `splashScreenImageDark` field plus the existing bootstrap fields `lightThemeBackgroundColor` / `darkThemeBackgroundColor` in `UnoAppManifest.js` (reusing the bootstrap's existing CSS-variable theme pipeline rather than inventing a new color field). Legacy `Color` continues to work as an alias for `BackgroundColor`; partial dark declarations fall back per-attribute to the light values; missing `DarkImage` files produce a clear build error; and when no background is declared, resizetizer emits fixed Uno-aligned defaults (`#F3F3F3` light, `#202020` dark) directly into all generated artifacts. A companion change in `Uno.Wasm.Bootstrap` (branch `dev/mazi/theme-aware-splash`) adds `matchMedia`-based image selection and suppresses the legacy inline `splashScreenColor` path when per-theme colors are present.

## Technical Context

**Language/Version**: C# (.NET Standard 2.0 for the MSBuild tasks assembly; build-time only — consuming apps target .NET 8/9 for Android, iOS, WASM)
**Primary Dependencies**: Microsoft.Build.Framework / Microsoft.Build.Utilities.Core (MSBuild task API), SkiaSharp (image rasterization / color parsing), System.Xml / System.Text.Json (artifact emission). No new runtime dependencies.
**Storage**: N/A — all state is build-time intermediate files under `$(IntermediateOutputPath)unoresizetizer\sp\` (drawable XML, storyboard, `UnoAppManifest.js`, partial `UnoInfo.plist`).
**Testing**: xUnit via `MSBuildTaskFixture` harness in `src/Resizetizer/test/UnitTests/` — tasks are instantiated and `Execute()`d directly; generated output is asserted against golden XML/text in `testdata/`. New golden files added for dark-variant expected outputs.
**Target Platform**: MSBuild task assembly runs on any .NET Standard 2.0 host (Windows, macOS, Linux, inside `dotnet build` and VS). Generated artifacts target: Android API 31+ (night qualifier), iOS 13+ (storyboard + xcassets appearance variants), WASM (browser via `Uno.Wasm.Bootstrap`).
**Project Type**: Build-time library (MSBuild task pack) + accompanying `.targets` files in `src/.nuspec/*.targets`. Not a runtime library, not a web/mobile app.
**Performance Goals**: No measurable perf change vs. today. Incremental build stamps (`_UnoSplashStampFile`, `_UnoManifestStampFile`) MUST still short-circuit unchanged inputs. Adding dark variants MUST at most double the per-build splash work (one extra rasterization of `DarkImage`; no extra work when dark metadata is absent).
**Constraints**:
- Byte-level output equivalence with pre-feature output when no dark metadata is declared (SC-002).
- Wire-protocol forward compatibility: WASM manifest field names format-neutral (paths, not extensions) to accommodate future SVG passthrough (uno.resizetizer#259).
- Android filename format-neutrality: same filenames as light counterparts in night-qualified folders so a future VectorDrawable swap (uno.resizetizer#258) does not churn paths.
- No new runtime code in consuming apps — declaration is purely `.csproj` metadata.
- Splash is rendered by the OS before app code runs; runtime `RequestedTheme` overrides MUST NOT be considered.
- Pre-API-31 Android: dark variant silently skipped (documented limitation).
- Out-of-scope platforms (Windows/WinAppSDK, Skia desktops, macOS Catalyst): build MUST NOT error when dark metadata is declared; dark metadata is silently ignored and light-only output is produced (FR-015).
**Scale/Scope**: One new public metadata surface (three attributes on `UnoSplashScreen`); three MSBuild tasks modified (`GenerateSplashAndroidResources_v0`, `GenerateSplashStoryboard_v0`, `GenerateWasmSplashAssets_v0`); one shared image-info model extended (`ResizeImageInfo`); one shared resizer entry point extended (`ResizetizeImages_v0` to rasterize the `DarkImage` into a parallel Android drawable asset and iOS `.xcassets` dark variant). Documentation updates to two files (`using-uno-resizetizer.md`, `uno-resizetizer-properties.md`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The repository's `.specify/memory/constitution.md` is an unfilled template (placeholder principles `[PRINCIPLE_1_NAME]` through `[PRINCIPLE_5_NAME]`, no ratified version). There are therefore no ratified constitutional gates to evaluate for this feature. This plan adheres to the project's implicit conventions already evident in the codebase:

- **No new projects / no new runtime deps**: tasks remain inside the existing `Resizetizer` assembly; no new NuGet references are introduced.
- **Task-level unit tests**: every task modified or added is covered by xUnit tests in `test/UnitTests/` using the existing `MSBuildTaskFixture` harness, consistent with `GenerateSplashAndroidResourcesTests`, `GenerateSplashStoryboardTests`, etc.
- **Backward compatibility**: existing `Color` attribute and legacy item metadata continue to work byte-identically when no dark metadata is declared (SC-002).
- **Documentation-in-repo**: author-facing surface changes are documented in `doc/using-uno-resizetizer.md` and `doc/uno-resizetizer-properties.md` in the same PR (FR-016).

No violations; no entries required in Complexity Tracking.

**Action item (non-blocking for this feature)**: ratify `/.specify/memory/constitution.md` with real principles in a separate effort so future `/speckit.plan` runs have real gates to evaluate.

## Project Structure

### Documentation (this feature)

```text
specs/001-theme-aware-splash/
├── plan.md              # This file
├── spec.md              # Feature specification (already authored)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── msbuild-item.md                 # UnoSplashScreen authoring surface
│   ├── android-output.md               # drawable/, drawable-v31/, values/, values-night/, drawable-night-v31/
│   ├── ios-output.md                   # storyboard + xcassets appearance variants + partial Info.plist
│   └── wasm-appmanifest.md             # UnoAppManifest.js field set
└── tasks.md             # NOT created by /speckit.plan (Phase 2, via /speckit.tasks)
```

### Source Code (repository root)

This repo is a single MSBuild-task library plus sample consumer apps and docs. There is no "frontend/backend" split and no new project is introduced by this feature. The concrete touched paths are:

```text
src/
├── Resizetizer/
│   ├── src/                                            # task assembly (netstandard2.0)
│   │   ├── ResizeImageInfo.cs                          # extend: DarkFilename, DarkColor, parser support for DarkImage/DarkBackgroundColor/BackgroundColor(+Color alias)
│   │   ├── GenerateSplashAndroidResources.cs           # extend: emit values-night/uno_colors.xml + drawable-night-v31/uno_splash_image.xml
│   │   ├── GenerateSplashStoryboard.cs                 # extend: reference xcassets color + image whose entries carry appearance-trait dark variants; emit those entries
│   │   ├── GenerateWasmSplashAssets.cs                 # extend: emit splashScreenImage/splashScreenColor + splashScreenImageDark/splashScreenColorDark
│   │   ├── ResizetizeImages.cs                         # extend: rasterize DarkImage alongside the default image for Android (PNG per DPI) and iOS (xcassets light+dark appearances)
│   │   └── Resources/
│   │       └── UnoSplash.storyboard                    # template already uses single imageView + backgroundColor; keep as-is, let xcassets carry the theme variant
│   └── test/
│       └── UnitTests/
│           ├── GenerateSplashAndroidResourcesTests.cs  # add: dark metadata writes values-night + drawable-night-v31; missing dark writes no night folders
│           ├── GenerateSplashStoryboardTests.cs        # add: xcassets entries carry light+dark appearances when DarkImage/DarkBackgroundColor set
│           ├── GenerateWasmSplashAssetsTests.cs        # ADD FILE: asserts full manifest shape incl. dark fields + fixed defaults
│           ├── ResizeImageInfoTests.cs                 # add: parses DarkImage/DarkBackgroundColor, Color↔BackgroundColor alias, conflict error, invalid color error, missing dark file error
│           └── testdata/
│               ├── androidsplash/
│               │   ├── uno_colors_dark.xml             # ADD: expected values-night output
│               │   └── uno_splash_image_v31_dark.xml   # ADD: expected drawable-night-v31 output
│               ├── iossplash/                          # ADD (or wherever existing iOS goldens live): xcassets JSON with appearance variants
│               └── wasmsplash/                         # ADD: expected UnoAppManifest.js text
└── .nuspec/
    ├── Uno.Resizetizer.android.targets                 # no structural change; GenerateSplashAndroidResources_v0 already receives @(UnoSplashScreen) which now carries dark metadata
    ├── Uno.Resizetizer.apple.targets                   # small addition: ensure DarkImage rasterization output is picked up as ImageAsset for xcassets
    └── Uno.Resizetizer.wasm.targets                    # no structural change

doc/
├── using-uno-resizetizer.md                            # add UnoSplashScreen dark-mode section (Android API 31+ caveat, OS-theme-not-RequestedTheme caveat)
└── uno-resizetizer-properties.md                       # document BackgroundColor, DarkBackgroundColor, DarkImage

samples/
└── NewTemplate/                                        # add a DarkImage + DarkBackgroundColor to the sample's UnoSplashScreen for manual verification (NOT automated in CI)
```

**Structure Decision**: Single MSBuild-task library. No new projects, no alternative structures considered (mobile/web split does not apply — this is a build-time library whose consumers happen to target mobile/web). All functional changes land in `src/Resizetizer/src/` and are covered by unit tests under `src/Resizetizer/test/UnitTests/`; author-facing documentation lands in `doc/`.

## Complexity Tracking

> Constitution has no ratified gates, so no violations to justify. Table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *(none)* | *(none)* | *(none)* |
