# Contract: Android splash output

**Audience**: `GenerateSplashAndroidResources_v0` implementers and `ResizetizeImages_v0` Android branch.

## Intermediate output root

`$(_UnoIntermediateSplashScreen)` (today: `$(IntermediateOutputPath)unoresizetizer\sp\`).

## File set

### Always (unchanged from today)

```
values/uno_colors.xml
drawable/uno_splash_image.xml
drawable-v31/uno_splash_image.xml
drawable-*dpi/{outputName}.png           # from ResizetizeImages on the light Include
```

### When `ResizeImageInfo.DarkColor` differs from `Color`

```
values-night/uno_colors.xml              # NEW
```

### When `ResizeImageInfo.DarkFilename` is non-null OR `DarkColor` differs from `Color`

```
drawable-night-v31/uno_splash_image.xml  # NEW; identical shape to drawable-v31/uno_splash_image.xml
```

### When `ResizeImageInfo.DarkFilename` is non-null

```
drawable-night-*dpi/{outputName}.png     # NEW; rasterized from DarkFilename, same outputName as light
```

## Filename rules

- Rasterized PNG filenames are identical across `drawable-*dpi/` and `drawable-night-*dpi/` folders.
- The drawable XML references `@drawable/{outputName}` (unchanged) — Android's resource resolver selects the night variant by qualifier.
- Color resource name is `uno_splash_color` in both `values/` and `values-night/` (unchanged).

## Byte-level equivalence

When neither `DarkColor` nor `DarkFilename` applies (i.e. the item declared no dark metadata and light background was not defaulted from the new `#F3F3F3`), the generated `values/uno_colors.xml`, `drawable/uno_splash_image.xml`, and `drawable-v31/uno_splash_image.xml` MUST be byte-identical to pre-feature output. Tests assert this via the existing golden-XML comparison.

**Note**: the new `#F3F3F3` default when `Color` is entirely absent *does* change output byte-wise vs. today's `#00000000` (transparent). This is a deliberate spec decision (FR-011). Projects that want to preserve the transparent behavior can declare `BackgroundColor="Transparent"` explicitly.

## Pre-API-31 behavior (FR-007)

- `drawable-night/uno_splash_image.xml` is NOT emitted.
- `drawable-night-*dpi/{outputName}.png` is NOT emitted (only `-v31` suffixed folders).
- Consumer apps on pre-31 devices render the light splash regardless of OS theme. Documented in `using-uno-resizetizer.md`.

## Consumer author snippet (unchanged)

The `Styles.xml` references `@color/uno_splash_color` and `@drawable/uno_splash_image`. Authors do not change their theme snippet to opt into dark; declaring the item metadata is sufficient.
