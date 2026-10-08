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

| Property Name            | Description                                                                                                                           |
|--------------------------|---------------------------------------------------------------------------------------------------------------------------------------|
| `Include`                | Used to insert the path of the Background image.                                                                                      |
| `ForegroundFile`         | Used to insert the path of the Foreground image                                                                                       |
| `ForegroundScale`        | Used to rescale the Foreground image, in order to fit on the app icon, it's a percentage value so `0.33` will be translated as 33%.   |
| `AndroidForegroundScale` | The same as ForegroundScale, but the value will be applied just for Android.                                                          |
| `WasmForegroundScale`    | The same as ForegroundScale, but the value will be applied just for Wasm                                                              |
| `WindowsForegroundScale` | The same as ForegroundScale, but the value will be applied just for Windows                                                           |
| `IOSForegroundScale`     | The same as ForegroundScale, but the value will be applied just for iOS                                                               |
| `SkiaForegroundScale`    | The same as ForegroundScale, but the value will be applied just for Skia targets                                                      |

> [!NOTE]
> The `<PLATFORM>ForegroundScale` (`AndroidForegroundScale`, `WasmForegroundScale`, etc) will override the global `ForegroundScale` value.

## UnoImage

| Property Name | Description                                                            |
|---------------|------------------------------------------------------------------------|
| `Include`     | Used to insert the path of the image asset, could be a `png` or `svg`. |

## UnoSplashScreen

| Property Name  | Description                                                                                                                 |
|----------------|-----------------------------------------------------------------------------------------------------------------------------|
| `Include`      | Used to insert the path of the image asset, could be a `png` or `svg`.                                                      |
| `Scale`        | Used to scale the image that will be used as SplashScreen. This property will be overridden by any platform-specific scale. |
| `AndroidScale` | Used to scale the image that will be used as SplashScreen on Android platform.                                              |
| `IOSScale`     | Used to scale the image that will be used as SplashScreen on iOS platform.                                                  |
| `WindowsScale` | Used to scale the image that will be used as SplashScreen on Windows platform.                                              |
| `WasmScale`    | Used to scale the image that will be used as SplashScreen on Wasm.                                                          |
| `SkiaScale`    | Used to scale the image that will be used as SplashScreen on Skia targets (GTK and WPF).                                    |
| `BackgroundColor` | Background color of the splash screen, used for all themes unless `DarkBackgroundColor` overrides it. Replaces `Color`, which is no longer supported on `UnoSplashScreen` (see the usage guide). |
| `DarkBackgroundColor` | Background color of the splash screen in dark theme. Currently used on WebAssembly only. See the dark theme section in the usage guide. |
| `DarkFile`     | Path of the image used as splash screen logo in dark theme (the `Include` file is the default one). Processed like the main image. Currently used on WebAssembly only. |
| `AccentColor`  | Accent color of the loader progress bar on WebAssembly, in both themes unless `DarkAccentColor` is set. When not set, the loader uses the WinUI default accent (`#0067C0` light, `#60CDFF` dark). Ignored on other platforms. |
| `DarkAccentColor` | Accent color of the loader progress bar in dark theme on WebAssembly. Ignored on other platforms. |

> [!NOTE]
> `DarkBackgroundColor`, `DarkFile`, `AccentColor` and `DarkAccentColor` are optional. When neither is set, the generated output is unchanged. `DarkFile` must have a different file name than the main splash image.
> On WebAssembly, a background color that is missing, empty, `transparent` or fully transparent (alpha 0) is treated as unset and no color is written to the manifest, so the bootstrapper uses its theme defaults. The same applies to `AccentColor` and `DarkAccentColor`.
