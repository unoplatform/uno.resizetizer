---
description: "Task list for Theme-Aware Splash Screens"
---

# Tasks: Theme-Aware Splash Screens

**Input**: Design documents in `specs/001-theme-aware-splash/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: Included — spec SC-003 explicitly requires automated tests validating generated output for all three platforms under both with-dark and light-only inputs.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- All paths are relative to repo root (`D:\Work\uno.resizetizer\`).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Zero new project scaffolding — everything lands in existing `src/Resizetizer/src/` and `src/Resizetizer/test/UnitTests/`. This phase only ensures the feature branch is clean and the existing solution builds.

- [X] T001 Verify `src/Resizetizer/uno.resizetizer.sln` builds cleanly on `main` before any changes: `dotnet build src/Resizetizer/uno.resizetizer.sln -c Debug`. Record baseline green state.
- [X] T002 Run the existing unit test suite and record baseline pass count: `dotnet test src/Resizetizer/test/UnitTests/Resizetizer.UnitTests.csproj -c Debug --logger "console;verbosity=minimal"`. All tests MUST pass before proceeding. This baseline is the reference for SC-002 (no regressions). **Baseline: 352 passed, 8 skipped.**

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend the shared `ResizeImageInfo` parse/default/validation pipeline that every platform generator consumes. US1, US2, and US3 all depend on this being complete.

**⚠️ CRITICAL**: No user story work (Phase 3+) can begin until this phase is complete.

- [X] T003 Extend `src/Resizetizer/src/ResizeImageInfo.cs` to add properties `DarkFilename`, `DarkColor`, `DarkIsVector`, `HasDarkOverride` per `specs/001-theme-aware-splash/data-model.md` §2. Do NOT change parse logic yet — property declarations only, so the compile graph is green before logic lands.
- [X] T004 In `src/Resizetizer/src/ResizeImageInfo.cs` `Parse(ITaskItem)`, implement `BackgroundColor`/`Color` alias handling: read `BackgroundColor` first; if empty, read `Color`; if both non-empty, throw `InvalidDataException` with message `"UnoSplashScreen item '{identity}' declares both Color and BackgroundColor; use only one (BackgroundColor is preferred)."`. Applies to FR-002 and the Color+BackgroundColor conflict error.
- [X] T005 Moved to `ApplySplashScreenDefaults()` instance method (not inline in Parse) because `IsSplashScreen` metadata is not set on items passed directly to splash generators. Generators call this helper after Parse. Default injection: when no `Color` declared, set `Color = #F3F3F3` (FR-011).
- [X] T006 Same `ApplySplashScreenDefaults()` also handles `DarkColor`: parsed from `DarkBackgroundColor` in Parse; if absent, defaults to `#202020` when `Color` was also defaulted, else falls back to the declared light color (FR-005). Invalid `DarkBackgroundColor` → `InvalidDataException` from Parse (FR-014).
- [X] T007 In `src/Resizetizer/src/ResizeImageInfo.cs` `Parse(ITaskItem)`, implement `DarkImage` resolution: read metadata, if non-empty resolve absolute OR relative to `DefiningProjectDirectory`; if the file does not exist, throw `FileNotFoundException` naming the `UnoSplashScreen` item identity and the declared path (FR-013). If empty, leave `DarkFilename = null`.
- [X] T008 [P] Added `ResizeImageInfoTests.DarkSplashParsing` with 12 tests covering all parsing/defaulting/fallback paths. All 12 pass; full suite still green (364 passed / 8 skipped — 12 more than baseline).

**Checkpoint**: `ResizeImageInfo` is the single source of truth for light/dark resolution. Generators in Phase 3+ just consume `info.Color`, `info.DarkColor`, `info.Filename`, `info.DarkFilename`, `info.HasDarkOverride` — they never second-guess defaults or parse metadata themselves.

---

## Phase 3: User Story 1 — Declare a dark-mode splash variant (Priority: P1) 🎯 MVP

**Goal**: An author declares `BackgroundColor` + `DarkBackgroundColor` + `DarkImage` and gets a working dark splash on Android (API 31+), iOS (13+), and WASM.

**Independent Test**: Build a sample project with a full light+dark triplet declared on `UnoSplashScreen`. Inspect the generated intermediate output:
- `values-night/uno_colors.xml` contains the dark color.
- `drawable-night-v31/uno_splash_image.xml` exists and references `@drawable/{outputName}`.
- `drawable-night-*dpi/{outputName}.png` exists and is visually the dark image.
- `Assets.xcassets/UnoSplashBackground.colorset/Contents.json` contains a `{appearances:[{value:"dark"}]}` entry with the dark color.
- `Assets.xcassets/UnoSplashImage.imageset/Contents.json` contains dark-appearance entries pointing at dark PNGs.
- `UnoAppManifest.js` contains `splashScreenImageDark` and `splashScreenColorDark` with the declared values.
- Manually deploy/run on each platform with OS in dark mode → dark splash renders (spec US1 acceptance scenarios 1–3).

### Tests for User Story 1

> Write tests FIRST, ensure they FAIL before implementation.

- [ ] T009 [P] [US1] Add test `XmlIsValidWithDarkColorAndDarkImage` to `src/Resizetizer/test/UnitTests/GenerateSplashAndroidResourcesTests.cs`. Input: `TaskItem` with `Include=appiconfg.svg`, `BackgroundColor=#FFFFFF`, `DarkBackgroundColor=#000000`, `DarkImage=appiconfg_dark.svg` (create `src/Resizetizer/test/UnitTests/images/appiconfg_dark.svg` as a simple variant). Assert: `values/uno_colors.xml` = light color, `values-night/uno_colors.xml` = dark color, `drawable-v31/uno_splash_image.xml` exists, `drawable-night-v31/uno_splash_image.xml` exists with identical shape. Add golden files `src/Resizetizer/test/UnitTests/testdata/androidsplash/uno_colors_dark.xml` and `uno_splash_image_v31_dark.xml`.
- [ ] T010 [P] [US1] Add test `StoryboardAndXcassetsWithDarkPair` to `src/Resizetizer/test/UnitTests/GenerateSplashStoryboardTests.cs`. Input: full triplet. Assert: storyboard references `<namedColor name="UnoSplashBackground"/>` and image `UnoSplashImage`; `UnoSplashBackground.colorset/Contents.json` has two color entries (one with `appearances:[{value:"dark"}]`); `UnoSplashImage.imageset/Contents.json` has dark-appearance entries. Add iOS golden files under `src/Resizetizer/test/UnitTests/testdata/iossplash/`.
- [ ] T011 [P] [US1] Create `src/Resizetizer/test/UnitTests/GenerateSplashWasmAssetsTests.cs` (NEW FILE). Fixture: `MSBuildTaskTestFixture<GenerateWasmSplashAssets_v0>`. Test `ManifestHasDarkFieldsWithFullTriplet`: input task item with full triplet + an `EmbeddedResource` pointing at a minimal `AppManifest.js` template. Assert generated `UnoAppManifest.js` contains `splashScreenImage` + `splashScreenImageDark` + `lightThemeBackgroundColor` + `darkThemeBackgroundColor` with the declared values, AND does NOT contain `splashScreenColor` (suppressed when dark metadata is declared — see `contracts/wasm-appmanifest.md`). Create fixture AppManifest fragment in `testdata/wasmsplash/AppManifest.input.js`.

### Implementation for User Story 1

- [ ] T012 [US1] Extend `src/Resizetizer/src/GenerateSplashAndroidResources.cs`:
    1. In `WriteColors(info, tools)`, after writing `values/uno_colors.xml`, if `info.DarkColor.HasValue && info.DarkColor != info.Color`, write a parallel `values-night/uno_colors.xml` containing `<color name="uno_splash_color">{DarkColor hex}</color>`.
    2. Add `WriteDrawable_v31_Night(info, tools)` that, when `info.HasDarkOverride`, emits `drawable-night-v31/uno_splash_image.xml` with shape identical to the light `drawable-v31/uno_splash_image.xml` (same `@color/uno_splash_color` reference, same `@drawable/{outputName}` reference, same sized bitmap).
    3. Deliberately do NOT emit `drawable-night/uno_splash_image.xml` (pre-31, out of scope per FR-007).
    Depends on T003–T007 (Phase 2).
- [ ] T013 [US1] Extend `src/Resizetizer/src/ResizetizeImages.cs` Android branch to rasterize `info.DarkFilename` into `drawable-night-*dpi/{outputName}.png` for each Android DPI when `info.DarkFilename != null`. Reuse the existing `SkiaSharpTools.Create(...)` / resize pipeline; only change is routing the output path. If `DarkFilename == null` but `DarkColor` differs, skip raster (Android layer-list references `@drawable/{outputName}` and resolves to the light raster automatically — intentional fallback).
- [ ] T014 [US1] Rewrite `src/Resizetizer/src/Resources/UnoSplash.storyboard` to reference named assets: replace the `<color key="backgroundColor" ...>` literal with `<namedColor key="backgroundColor" name="UnoSplashBackground"/>`; replace `image="{imageView.image}"` with `image="UnoSplashImage"`; remove the `<resources><image name="{0}"/></resources>` block. Remove `{color.*}` placeholders — the color now lives in the colorset and is no longer substituted.
- [ ] T015 [US1] Simplify `src/Resizetizer/src/GenerateSplashStoryboard.cs` `SubstituteStoryboard(...)` to only copy the template verbatim (no `{color.*}` or `{imageView.image}` substitutions remain after T014). Remove the unused `r`/`g`/`b`/`a` parameters from the method signature and the `Execute()` call site.
- [ ] T016 [US1] Add new logic in `src/Resizetizer/src/GenerateSplashStoryboard.cs` `Execute()` to write `Assets.xcassets/UnoSplashBackground.colorset/Contents.json` and `Assets.xcassets/UnoSplashImage.imageset/Contents.json` under the intermediate storyboard directory. Both always include the light entry; when `info.DarkColor != info.Color` → colorset adds a dark-appearance entry; when `info.DarkFilename != null` → imageset adds dark-appearance per-DPI entries. Use `System.Text.Json` (no new dependency) for JSON emission.
- [ ] T017 [US1] Extend `src/Resizetizer/src/ResizetizeImages.cs` Apple branch to rasterize `info.DarkFilename` into the `UnoSplashImage.imageset/` folder with suffix `{outputName}_dark@{scale}x.png` for each iOS scale when `info.DarkFilename != null`. The imageset `Contents.json` emitted in T016 references these filenames for the dark-appearance entries.
- [ ] T018 [US1] Verify `src/.nuspec/Uno.Resizetizer.apple.targets` pipes the new `.xcassets/UnoSplash*.imageset/` and `.xcassets/UnoSplashBackground.colorset/` files into the existing `ProcessResizedImagesApple_v0` flow (which already collects `Assets.xcassets\*` into `ImageAsset`). If the existing glob `_UnoResizetizerCollectedBundleResourceImages` does not pick up the new imageset/colorset Contents.json files, add a narrowly-scoped item inclusion in this targets file that adds them as `ImageAsset` with the correct `LogicalName`/`Link`. Document the decision inline.
- [ ] T019 [US1] Extend `src/Resizetizer/src/GenerateWasmSplashAssets.cs` `ProcessAppManifestFile(...)` to emit two mutually-exclusive modes per `contracts/wasm-appmanifest.md`:
    1. Always emit `dic["splashScreenImage"]` (existing key, value from `info.OutputName`).
    2. When `info.HasDarkOverride` is FALSE (neither `DarkBackgroundColor` nor `DarkImage` declared): emit `dic["splashScreenColor"] = $"\"{Utils.SkiaColorWithoutAlpha(info.Color)}\""` as today — byte-compat path.
    3. When `info.HasDarkOverride` is TRUE: emit `dic["lightThemeBackgroundColor"] = $"\"{Utils.SkiaColorWithoutAlpha(info.Color)}\""` and `dic["darkThemeBackgroundColor"] = $"\"{Utils.SkiaColorWithoutAlpha(info.DarkColor)}\""`; do NOT emit `splashScreenColor` (it would clobber the bootstrap's `@media (prefers-color-scheme: dark)` rule via inline style). If `FindWhatINeed` parsed a user-declared `splashScreenColor` out of the input `AppManifest.js`, remove it from the dictionary before writing.
    4. When `info.DarkFilename != null`: emit `dic["splashScreenImageDark"]` (naming convention: reuse `OutputName` + `_dark` suffix — add helper on `ResizeImageInfo` if needed). When `info.DarkFilename == null`, do NOT emit the key (absent-key contract).
- [ ] T020 [US1] Extend `src/Resizetizer/src/ResizetizeImages.cs` WASM branch to rasterize `info.DarkFilename` into the WASM splash-screen output folder alongside the light image with filename convention matching what `GenerateWasmSplashAssets` references (from T019): `{outputName}_dark.scale-{N}.png`. Ensure the generated file is picked up by the existing `ProcessResizedImagesWasm` target so it is included in the WASM content output.
- [ ] T021 [US1] Update sample project `samples/NewTemplate/Resizetizer.Extensions.Sample.Base/base.props` `UnoSplashScreen` item to add `DarkBackgroundColor="#202020"` and `DarkImage="$(MSBuildThisFileDirectory)SplashScreen\splash_screen_dark.svg"` (create a simple inverted-variant SVG at `samples/NewTemplate/Resizetizer.Extensions.Sample.Base/SplashScreen/splash_screen_dark.svg`). This is the manual verification vehicle for SC-001. Do not wire this into CI.
- [ ] T022 [US1] Run the T009–T011 tests end-to-end (`dotnet test src/Resizetizer/test/UnitTests/Resizetizer.UnitTests.csproj --filter "FullyQualifiedName~Dark|FullyQualifiedName~UnoSplashBackground|FullyQualifiedName~splashScreenImageDark"`). All MUST pass. If any fail, return to T012–T020 to fix.

**Checkpoint**: US1 is fully functional. With a full light+dark triplet declared, an author gets correct dark-mode splash output on Android, iOS, and WASM. Manual verification via sample from T021 on one device/simulator per platform satisfies SC-001.

---

## Phase 4: User Story 2 — Backward-compatible upgrade (Priority: P1)

**Goal**: Existing projects that use only `Color` (or only a default image, no dark metadata) build and render identically to pre-feature behavior. Zero regressions.

**Independent Test**: Take the sample project's legacy configuration (`UnoSplashScreen Include="splash.svg" Color="#512BD4"` with no dark metadata). Generated output:
- Android: `values/uno_colors.xml` = `#512BD4`, `drawable/`, `drawable-v31/` byte-identical to baseline. No `values-night/`, no `drawable-night-*/` emitted.
- iOS: named colorset / imageset emitted BUT each contains only the light entry (no `appearances` key). Storyboard structurally references named assets (new — no byte-identity, covered by structural-equivalence clause of SC-002).
- WASM: `UnoAppManifest.js` byte-compat mode — `splashScreenImage` + `splashScreenColor` exactly as today; `splashScreenImageDark`, `lightThemeBackgroundColor`, and `darkThemeBackgroundColor` all absent.
  - **Note on the WASM byte-compat path**: when no dark metadata is declared, `GenerateWasmSplashAssets_v0` takes the legacy code path (Mode A per `contracts/wasm-appmanifest.md`) and emits only `splashScreenImage` + `splashScreenColor`. Theme-aware bootstrap fields are NOT emitted, preserving byte-equivalence with pre-feature output.

### Tests for User Story 2

- [ ] T023 [P] [US2] Add test `XmlIsIdenticalWhenOnlyLegacyColorDeclared` to `src/Resizetizer/test/UnitTests/GenerateSplashAndroidResourcesTests.cs`. Input: `TaskItem` with only `Include` + `Color="#512BD4"` (no `BackgroundColor`, no `DarkBackgroundColor`, no `DarkImage`). Assert byte-equivalence of `values/uno_colors.xml`, `drawable/uno_splash_image.xml`, `drawable-v31/uno_splash_image.xml` with the existing pre-feature goldens (the existing `XmlIsValid` test's golden files serve as the reference). Additionally assert `values-night/uno_colors.xml`, `drawable-night-v31/uno_splash_image.xml`, and `drawable-night-*dpi/` do NOT exist.
- [ ] T024 [P] [US2] Add test `StoryboardStructurallyEquivalentWhenNoDark` to `src/Resizetizer/test/UnitTests/GenerateSplashStoryboardTests.cs`. Input: only `Include` + `Color`. Assert the storyboard references named assets (new) but the emitted `UnoSplashBackground.colorset/Contents.json` contains exactly ONE color entry (no `appearances` key), and the `UnoSplashImage.imageset/Contents.json` contains only light-appearance entries. Add golden files `src/Resizetizer/test/UnitTests/testdata/iossplash/colorset_light_only.json` and `imageset_light_only.json`.
- [ ] T025 [P] [US2] In `src/Resizetizer/test/UnitTests/GenerateSplashWasmAssetsTests.cs` (created in T011), add test `ManifestByteCompatWhenNoDarkMetadata`: only `Include` + `Color="#512BD4"`. Assert generated `UnoAppManifest.js` contains `splashScreenImage` + `splashScreenColor` (= `#512BD4`) and does NOT contain `splashScreenImageDark`, `lightThemeBackgroundColor`, or `darkThemeBackgroundColor`. This is the byte-compat mode.
- [ ] T026 [P] [US2] Add test `NoDarkMetadataProducesNoWarnings` to a new `src/Resizetizer/test/UnitTests/SplashRegressionTests.cs` (shared across generators). Drive each generator with only `Include`+`Color` and assert `LogWarningEvents` is empty and `LogErrorEvents` is empty (FR of US2 AC2).

### Implementation for User Story 2

No new production code is expected here — US2 is validated entirely by the tests above. The implementation steps under US1 (T012, T016, T019) explicitly gate dark output on `info.HasDarkOverride` / `info.DarkFilename != null` / `info.DarkColor != info.Color`, producing the backward-compatible path by construction.

- [ ] T027 [US2] If any US2 test in T023–T026 fails, patch the relevant generator's gating condition (in `GenerateSplashAndroidResources.cs`, `GenerateSplashStoryboard.cs`, or `GenerateWasmSplashAssets.cs`) so the no-dark case takes the pre-feature code path. Root-cause only; do not add shim behavior.
- [ ] T028 [US2] Run full existing test suite: `dotnet test src/Resizetizer/test/UnitTests/Resizetizer.UnitTests.csproj -c Debug`. Compare pass count to T002 baseline. All pre-existing tests MUST still pass. This validates SC-002.

**Checkpoint**: US2 validated. Existing projects upgrade cleanly. US1 + US2 both work independently.

---

## Phase 5: User Story 3 — Partial dark declarations degrade gracefully (Priority: P2)

**Goal**: Declaring only `DarkBackgroundColor` (no `DarkImage`), or only `DarkImage` (no `DarkBackgroundColor`), produces correct dark-mode visuals with per-attribute fallback.

**Independent Test**: Declare each partial combination and verify output:
- `BackgroundColor` + `DarkBackgroundColor` only → Android: `values-night/` emitted, no `drawable-night-*/` emitted (layer-list resolves `@drawable/{outputName}` to light raster). iOS: colorset has dark entry, imageset has only light entries. WASM: `splashScreenColorDark` set, `splashScreenImageDark` absent.
- `BackgroundColor` + `DarkImage` only → Android: `drawable-night-v31/` + `drawable-night-*dpi/` emitted, no `values-night/` emitted (`@color/uno_splash_color` resolves to light). iOS: colorset only light, imageset has dark entries. WASM: `splashScreenImageDark` set, `splashScreenColorDark` = light color (fallback).

### Tests for User Story 3

- [ ] T029 [P] [US3] Add test `DarkColorOnlyEmitsValuesNightButNoNightDrawables` to `src/Resizetizer/test/UnitTests/GenerateSplashAndroidResourcesTests.cs`. Input: `BackgroundColor=#FFF`, `DarkBackgroundColor=#000`, no `DarkImage`. Assert `values-night/uno_colors.xml` exists with `#000`; `drawable-night-v31/uno_splash_image.xml` is NOT emitted; `drawable-night-*dpi/` is NOT emitted.
  - **Note**: this contradicts T012 step 2 as written (which emits `drawable-night-v31/` when `HasDarkOverride`). Resolve by tightening the condition in T012: emit `drawable-night-v31/` only when `DarkFilename != null` (not on color-only dark). Update T012 accordingly; keep this test as the regression guard.
- [ ] T030 [P] [US3] Add test `DarkImageOnlyEmitsNightDrawablesButNoValuesNight` to `src/Resizetizer/test/UnitTests/GenerateSplashAndroidResourcesTests.cs`. Input: `BackgroundColor=#FFF`, `DarkImage=splash_dark.svg`, no `DarkBackgroundColor`. Assert `drawable-night-v31/uno_splash_image.xml` exists; `drawable-night-*dpi/{outputName}.png` exists; `values-night/uno_colors.xml` is NOT emitted.
- [ ] T031 [P] [US3] Add test `ColorsetAndImagesetPartialDark` to `src/Resizetizer/test/UnitTests/GenerateSplashStoryboardTests.cs`. Two cases: (a) DarkColor only → colorset has dark entry, imageset light-only; (b) DarkImage only → colorset light-only, imageset has dark entries.
- [ ] T032 [P] [US3] In `GenerateSplashWasmAssetsTests.cs`, add tests: (a) `ManifestDarkColorOnly` — `BackgroundColor=#FFF` + `DarkBackgroundColor=#000`, no `DarkImage` → `lightThemeBackgroundColor=#FFF`, `darkThemeBackgroundColor=#000`, `splashScreenImageDark` absent, `splashScreenColor` absent; (b) `ManifestDarkImageOnly` — `BackgroundColor=#FFF` + `DarkImage=splash_dark.svg`, no `DarkBackgroundColor` → `lightThemeBackgroundColor=#FFF`, `darkThemeBackgroundColor=#FFF` (fallback to light), `splashScreenImageDark` set, `splashScreenColor` absent.

### Implementation for User Story 3

- [ ] T033 [US3] Adjust `src/Resizetizer/src/GenerateSplashAndroidResources.cs` gate conditions: emit `values-night/uno_colors.xml` iff `info.DarkColor != info.Color`; emit `drawable-night-v31/uno_splash_image.xml` iff `info.DarkFilename != null`. Per-attribute gating supersedes T012's coarser `HasDarkOverride` gate.
- [ ] T034 [US3] Confirm `GenerateSplashStoryboard.cs` T016 implementation already gates colorset dark entry on `info.DarkColor != info.Color` and imageset dark entries on `info.DarkFilename != null`, per-attribute. If T016 used `HasDarkOverride` for either, tighten to per-attribute.
- [ ] T035 [US3] Confirm `GenerateWasmSplashAssets.cs` T019 implementation: (a) emits `splashScreenImageDark` iff `info.DarkFilename != null`; (b) in Mode B (`HasDarkOverride == true`) always emits both `lightThemeBackgroundColor` and `darkThemeBackgroundColor` (with per-attribute fallback resolved in T006); (c) in Mode B suppresses `splashScreenColor`. Test T032 validates.
- [ ] T036 [US3] Run the T029–T032 tests. All MUST pass. If any fail, return to T033–T035.

**Checkpoint**: All three user stories independently functional. Partial declarations produce the documented fallback behavior on every platform.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and cross-platform integration checks required by SC-005 and FR-016.

- [ ] T037 [P] Update `doc/using-uno-resizetizer.md`'s `UnoSplashScreen` section to document: (a) new `BackgroundColor` attribute with example, (b) `Color` as legacy alias, (c) `DarkBackgroundColor` and `DarkImage` with example triplet, (d) Android API 31+ caveat, (e) splash tracks OS theme not app `RequestedTheme`, (f) default values `#F3F3F3` / `#202020` when no background declared (FR-016, SC-005).
- [ ] T038 [P] Update `doc/uno-resizetizer-properties.md` to list `BackgroundColor`, `DarkBackgroundColor`, `DarkImage` under `UnoSplashScreen` with types, defaults, and cross-references to the how-to in `using-uno-resizetizer.md` (FR-016).
- [ ] T039 Run `specs/001-theme-aware-splash/quickstart.md` end-to-end against the sample from T021 on at least one device/simulator for each of Android API 31+, iOS 13+, and WASM (browser). Document the observed splash for light and dark OS themes in a short validation note attached to the PR. This is the manual check for SC-001.
- [ ] T040 Incremental-build check: make a change to only `DarkBackgroundColor` on the sample's `UnoSplashScreen` item, rebuild, and confirm the `_UnoSplashStampFile` / `_UnoManifestStampFile` correctly invalidate (i.e. splash tasks re-run). If stamps don't invalidate, update stamp inputs in `src/.nuspec/Uno.Resizetizer.targets` to include the new metadata.
- [ ] T041 Final pass: `dotnet build src/Resizetizer/uno.resizetizer.sln -c Release` + `dotnet test ... -c Release`. Release build and tests must be green. Compare release output file set against T002 baseline for projects with no dark metadata → must show zero diff in the baseline-covered files (SC-002).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: no deps — run first.
- **Phase 2 (Foundational)**: depends on Phase 1. **Blocks** all user stories.
- **Phase 3 (US1)**: depends on Phase 2. Delivers the feature's headline value.
- **Phase 4 (US2)**: depends on Phase 2. Can run in parallel with Phase 3 after T012/T016/T019 are in; T023–T028 validate regressions against the US1 gating.
- **Phase 5 (US3)**: depends on Phase 3 (US3 tightens the US1 implementation's gating). Cannot fully parallelize with US1.
- **Phase 6 (Polish)**: depends on US1, US2, US3 complete.

### Within-Story Ordering (critical edges)

- T003 → T004–T007 (properties exist before parse logic).
- T004–T007 → T008 (tests exist before green; may be authored in parallel with T004–T007 but must run and fail first, then pass).
- T012 → T013 (Android drawable XML references `@drawable/{outputName}` which T013 must have produced for the dark raster to resolve).
- T014 → T015 → T016 → T017 → T018 (storyboard template → substitution simplification → xcassets emission → dark raster → targets wiring).
- T019 → T020 (WASM manifest emits the filename T020 must produce).
- T012 → T029 gate refinement in T033 (US3 tightens US1).

### Parallel Opportunities

- **Within Phase 2**: T008 test authoring in parallel with T004–T007 implementation.
- **Within Phase 3 (US1) tests**: T009, T010, T011 are in three separate test files → all `[P]`.
- **Within Phase 3 (US1) impl**: Android chain (T012+T013) and WASM chain (T019+T020) and iOS chain (T014→T015→T016→T017→T018) touch disjoint files and can run in parallel by platform.
- **Across US2 tests**: T023, T024, T025, T026 are in different test files → all `[P]`.
- **Across US3 tests**: T029, T030 share `GenerateSplashAndroidResourcesTests.cs` (same file) — NOT `[P]` with each other; T031, T032 are in different files → `[P]` with T029/T030 and each other.
- **Polish**: T037 and T038 touch different doc files → `[P]`.

---

## Parallel Example: User Story 1

```text
# Three test files can be authored in parallel (tests first):
Task T009 [P] [US1]: GenerateSplashAndroidResourcesTests — XmlIsValidWithDarkColorAndDarkImage
Task T010 [P] [US1]: GenerateSplashStoryboardTests — StoryboardAndXcassetsWithDarkPair
Task T011 [P] [US1]: Create GenerateSplashWasmAssetsTests — ManifestHasDarkFieldsWithFullTriplet

# Three platform implementation chains can progress in parallel:
Android: T012 → T013
WASM:    T019 → T020
iOS:     T014 → T015 → T016 → T017 → T018
```

---

## Implementation Strategy

### MVP First (US1 only)

1. Phase 1 (Setup: T001–T002).
2. Phase 2 (Foundational: T003–T008) — **critical path**, blocks everything.
3. Phase 3 (US1: T009–T022).
4. **STOP and VALIDATE**: run T039 manually on one device per platform with a full triplet declared. If splash flips on OS theme toggle, MVP is green.
5. Ship coordinated with a compatible `Uno.Wasm.Bootstrap` release.

### Incremental Delivery

1. Setup + Foundational → infrastructure.
2. + US1 → MVP (feature ships; Uno.Wasm.Bootstrap coordinated).
3. + US2 → regression-safety net (T023–T028 catch any byte drift).
4. + US3 → partial-declaration support (T029–T036 lock in per-attribute fallback).
5. Polish (T037–T041) → docs, manual validation, incremental-build integrity.

### Parallel Team Strategy

With 2 developers after Phase 2:

- Dev A: US1 (Phase 3) end-to-end.
- Dev B: author all US2 + US3 tests (T023–T026, T029–T032) as red-failing regressions in parallel; when Dev A completes T012/T016/T019, Dev B tightens gates per T033–T036 and verifies green.

With 3 developers: split US1 by platform (Android / iOS / WASM) after T003–T008 land.

---

## Notes

- All tasks are scoped to a single file or a narrow group of files documented inline. No task is vague.
- `[P]` means "file-disjoint with the other `[P]` tasks in the same group" — explicit in each task's target path.
- Every task traces to a spec requirement: FR-###, SC-###, or a named user story AC.
- Out-of-scope platforms (Windows/WinAppSDK, Skia desktop, Catalyst) are NOT addressed here — FR-015's silent-fallback requirement is already satisfied by existing targets' platform gates (Android/Apple/WASM targets never run on those platforms).
- Commit after each task or logical group; stop at each Checkpoint to validate.
