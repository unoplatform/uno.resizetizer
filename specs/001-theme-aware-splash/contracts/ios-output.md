# Contract: iOS splash output

**Audience**: `GenerateSplashStoryboard_v0` implementers and `ResizetizeImages_v0` Apple branch.

## Intermediate output root

`$(_UnoIntermediateSplashScreen)` (today: `$(IntermediateOutputPath)unoresizetizer\sp\`).

## File set

### Always

```
UnoSplash.storyboard                                      # existing; references named assets
UnoInfo.plist                                             # existing partial plist, unchanged
Assets.xcassets/UnoSplashBackground.colorset/Contents.json   # NEW file, even in the no-dark case — stores the light color as a named color asset so the storyboard can reference it
Assets.xcassets/UnoSplashImage.imageset/Contents.json        # NEW — existing raster PNGs are moved into this imageset's entry list
Assets.xcassets/UnoSplashImage.imageset/{outputName}@1x.png
Assets.xcassets/UnoSplashImage.imageset/{outputName}@2x.png
Assets.xcassets/UnoSplashImage.imageset/{outputName}@3x.png
```

### Additionally when `DarkColor` differs from `Color`

`UnoSplashBackground.colorset/Contents.json` includes a second entry:

```json
{
  "appearances": [{ "appearance": "luminosity", "value": "dark" }],
  "color": { "color-space": "srgb", "components": { ... } },
  "idiom": "universal"
}
```

### Additionally when `DarkFilename` is non-null

`UnoSplashImage.imageset/Contents.json` includes per-DPI entries carrying `"appearances": [{"appearance":"luminosity","value":"dark"}]`, and the corresponding dark PNG files are added alongside the light ones. Naming convention: append `_dark` to the filename inside the imageset (the `Contents.json` references them by name, so the name is local to the imageset and does not need to match any global asset name).

## Storyboard reference changes

The storyboard template (`src/Resizetizer/src/Resources/UnoSplash.storyboard`) replaces:

- `<color key="backgroundColor" ...>` → `<namedColor name="UnoSplashBackground"/>`
- `<imageView ... image="{imageView.image}">` → `<imageView ... image="UnoSplashImage">`
- The trailing `<resources><image name="{0}"/></resources>` block is dropped (the image is a named asset now).

Placeholder substitutions (`{color.red}` etc.) are removed from `SubstituteStoryboard` since color now lives in the colorset.

## Partial `Info.plist`

Unchanged — still points `UILaunchStoryboardName` at the storyboard. iOS resolves appearance traits when loading the named assets.

## Structural equivalence rather than byte equivalence

Moving from literal-color + bare-filename image references to named-asset references is a genuine change to the storyboard's bytes. Byte-identical output with today is not achievable; instead:

- Tests assert the storyboard references the named assets exactly.
- The no-dark golden output is captured in `testdata/iossplash/` and becomes the new reference.
- Consumers' launch visuals are unchanged (light mode renders the same color and same image as before), satisfying SC-002's "structural equivalence" clause.

## Catalyst

`maccatalyst` platform: no splash generation (existing guard `'$(TargetPlatformIdentifier)' != 'maccatalyst'` in `Uno.Resizetizer.apple.targets` stays). Dark metadata is silently ignored on Catalyst (FR-015).
