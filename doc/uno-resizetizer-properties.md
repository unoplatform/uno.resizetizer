---
uid: Overview.Uno.Resizetizer.Properties
---

# Resizetizer Properties

Resizetizer has support for the following three items:

* `UnoIcon`
* `UnoImage`
* `UnoSplashScreen`

Each of these items has properties that allow you to influence the behavior of the generated asset. On this page, you can find all available properties for each item and their meanings.

## Global Properties

Properties that can be used across all items

| Property Name | Description                                                                                                                         |
|---------------|-------------------------------------------------------------------------------------------------------------------------------------|
| `BaseSize`    | Used to specify the size that will be used as a basement for the resize operations. e.g.: `BaseSize="48,48"`                        |
| `Link`        | Used to specify a custom path for your image, this path should be used inside your application when you want to reference the image |
| `Resize`      | Boolean value to say if the asset should be Resized or not. By default, just vector assets are resized by default                   |
| `TintColor`   | Color that will be used to tint the image during the resize phase. You can use a Hex value or a named value like `Fuchsia`          |
| `Color`       | Color that will be used as a background color                                                                                       |

## UnoIcon

| Property Name              | Description                                                                                                                                                                                            |
|----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Include`                  | Used to insert the path of the Background image.                                                                                                                                                       |
| `ForegroundFile`           | Used to insert the path of the Foreground image                                                                                                                                                        |
| `ForegroundScale`          | Used to rescale the Foreground image, in order to fit on the app icon, it's a percentage value so `0.33` will be translated as 33%.                                                                    |
| `AndroidForegroundScale`   | The same as ForegroundScale, but the value will be applied just for Android.                                                                                                                           |
| `IOSForegroundScale`       | The same as ForegroundScale, but the value will be applied just for iOS.                                                                                                                               |
| `TvOSForegroundScale`      | The same as ForegroundScale, but the value will be applied just for tvOS.                                                                                                                              |
| `WasmForegroundScale`      | The same as ForegroundScale, but the value will be applied just for WebAssembly.                                                                                                                       |
| `WinAppSdkForegroundScale` | The same as ForegroundScale, but the value will be applied just for the Windows App SDK head. Replaces `WindowsForegroundScale`.                                                                       |
| `DesktopForegroundScale`   | The same as ForegroundScale, but the value will be applied just for the Desktop (Skia) head on Windows, macOS and Linux. Replaces `SkiaForegroundScale`.                                               |
| `WinAppSdkAppIconScale`    | Size of the foreground on the plated Windows App SDK app icon (`Logo.scale-*`, `Logo.targetsize-*`), as a share of the image. Default `0.75`. See [Windows App SDK padding](#windows-app-sdk-padding). |
| `WinAppSdkSmallTileScale`  | Size of the foreground on the Windows App SDK small tile (`SmallTile.scale-*`). Default `0.5`.                                                                                                         |
| `WinAppSdkTileScale`       | Size of the foreground on the Windows App SDK medium, wide and large tiles. Default `0.33`.                                                                                                            |
| `WinAppSdkStoreLogoScale`  | Size of the foreground on the Windows App SDK store logo (`StoreLogo.scale-*`). Default `1.0`.                                                                                                         |
| `WindowsForegroundScale`   | Legacy name of `WinAppSdkForegroundScale`. Still works; `WinAppSdkForegroundScale` wins when both are set.                                                                                             |
| `SkiaForegroundScale`      | Legacy name of `DesktopForegroundScale`. Still works; `DesktopForegroundScale` wins when both are set.                                                                                                 |

> [!NOTE]
> The `<PLATFORM>ForegroundScale` (`AndroidForegroundScale`, `WasmForegroundScale`, etc) will override the global `ForegroundScale` value.
>
> The `WinAppSdk*` names apply to the Windows App SDK head only. Desktop on Windows is the Desktop (Skia) head and uses the `Desktop*` names.

### Windows App SDK padding

On Windows App SDK, each generated image pads the foreground the way Visual Studio's asset generator does. The foreground is fitted to the shorter side of the image, then multiplied by the asset's scale below and by `ForegroundScale` (or `WinAppSdkForegroundScale`, which applies to every Windows App SDK image). The Desktop head on Windows is not padded; it only uses the `.ico`. The background (`Include` file or `Color`) still fills the whole image.

| Images                                                                          | Metadata                  | Default |
|---------------------------------------------------------------------------------|---------------------------|---------|
| `Logo.scale-*`, `Logo.targetsize-*` (plated app icon)                           | `WinAppSdkAppIconScale`   | `0.75`  |
| `Logo.altform-unplated_targetsize-*`, `Logo.altform-lightunplated_targetsize-*` | -                         | `1.0`   |
| `StoreLogo.scale-*`                                                             | `WinAppSdkStoreLogoScale` | `1.0`   |
| `SmallTile.scale-*`                                                             | `WinAppSdkSmallTileScale` | `0.5`   |
| `MediumTile.scale-*`, `WideTile.scale-*`, `LargeTile.scale-*`                   | `WinAppSdkTileScale`      | `0.33`  |

For example, with `ForegroundScale="0.8"` the medium tile's foreground is `0.33 × 0.8 = 0.264` of the tile, while the unplated taskbar icon stays at `0.8`. The splash screen is not affected; it keeps using `UnoSplashScreen`'s `Scale`.

To go back to the output of earlier versions, where the foreground fills every image, set all four to `1`. With Uno.Sdk, which adds the `UnoIcon` item for you, an item definition in the app's project file does it:

```xml
<ItemDefinitionGroup>
    <UnoIcon WinAppSdkAppIconScale="1" WinAppSdkSmallTileScale="1" WinAppSdkTileScale="1" WinAppSdkStoreLogoScale="1" />
</ItemDefinitionGroup>
```

## UnoImage

| Property Name | Description                                                            |
|---------------|------------------------------------------------------------------------|
| `Include`     | Used to insert the path of the image asset, could be a `png` or `svg`. |

## UnoSplashScreen

| Property Name         | Description                                                                                                                                                                                                                   |
|-----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Include`             | Used to insert the path of the image asset, could be a `png` or `svg`.                                                                                                                                                        |
| `Scale`               | Used to scale the image that will be used as SplashScreen. This property will be overridden by any platform-specific scale.                                                                                                   |
| `AndroidScale`        | Used to scale the image that will be used as SplashScreen on Android.                                                                                                                                                         |
| `IOSScale`            | Used to scale the image that will be used as SplashScreen on iOS.                                                                                                                                                             |
| `TvOSScale`           | Used to scale the image that will be used as SplashScreen on tvOS.                                                                                                                                                            |
| `WasmScale`           | Used to scale the image that will be used as SplashScreen on WebAssembly.                                                                                                                                                     |
| `WinAppSdkScale`      | Used to scale the image that will be used as SplashScreen on the Windows App SDK head. Replaces `WindowsScale`.                                                                                                               |
| `DesktopScale`        | Used to scale the image that will be used as SplashScreen on the Desktop (Skia) head on Windows, macOS and Linux. Replaces `SkiaScale`.                                                                                       |
| `BackgroundColor`     | Background color of the splash screen, used for all themes unless `DarkBackgroundColor` overrides it. Replaces `Color`, which is no longer supported on `UnoSplashScreen` (see the usage guide).                              |
| `DarkBackgroundColor` | Background color of the splash screen in dark theme. Currently used on WebAssembly only. See the dark theme section in the usage guide.                                                                                       |
| `DarkFile`            | Path of the image used as splash screen logo in dark theme (the `Include` file is the default one). Processed like the main image. Currently used on WebAssembly only.                                                        |
| `AccentColor`         | Accent color of the loader progress bar on WebAssembly, in both themes unless `DarkAccentColor` is set. When not set, the loader uses the WinUI default accent (`#0067C0` light, `#60CDFF` dark). Ignored on other platforms. |
| `DarkAccentColor`     | Accent color of the loader progress bar in dark theme on WebAssembly. Ignored on other platforms.                                                                                                                             |
| `WindowsScale`        | Legacy name of `WinAppSdkScale`. Still works; `WinAppSdkScale` wins when both are set.                                                                                                                                        |
| `SkiaScale`           | Legacy name of `DesktopScale`. Still works; `DesktopScale` wins when both are set.                                                                                                                                            |

> [!NOTE]
> `DarkBackgroundColor`, `DarkFile`, `AccentColor` and `DarkAccentColor` are optional. When neither is set, the generated output is unchanged. `DarkFile` must have a different file name than the main splash image.
> On WebAssembly, a background color that is missing, empty, `transparent` or fully transparent (alpha 0) is treated as unset and no color is written to the manifest, so the bootstrapper uses its theme defaults. The same applies to `AccentColor` and `DarkAccentColor`.
