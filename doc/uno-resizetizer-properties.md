---
uid: Overview.Uno.Resizetizer.Properties
---

# Resizetizer Properties

Resizetizer has support for three items they are:

* UnoIcon
* UnoImage
* UnoSplashScreen

Each of these items have properties that allow you to influence the behavior of the generated asset, at this page you can find all available properties for each item their meaning.

## Global Properties

Properties that can be used across all items

| Property Name | Description                                                                                                                         |
| ------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| BaseSize      | Used to specify the size that will be used as a basement for the resize operations. e.g.: `BaseSize="48,48"`                                     |
| Link          | Used to specify a custom path for your image, this path should be used inside your application when you want to reference the image |
| Resize        | Boolean value to say if the asset should Resized or not. By default just vectors asset are resized by default                       |
| TintColor     | Color that will be used to tint the image during the resize phase. You can use a Hex value or a named value like `Fuchsia`           |
| Color         | Color that will be used as a background color                                                                                         |

## UnoIcon

| Property Name          | Description                                                                                                                           |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Include                | Used to insert the path of the Background image.                                                                                      |
| ForegroundFile         | Used to insert the path of the Foreground image                                                                                       |
| ForegroundScale        | Used to rescale the Foreground image, in order to fit on the app icon, it's a percentage value so `0.33` will be translatated as 33%.   | 
| AndroidForegroundScale | The same as ForegroundScale, but the value will be applied just for Android.                                                          |
| WasmForegroundScale    | The same as ForegroundScale, but the value will be applied just for Wasm                                                              |
| WindowsForegroundScale | The same as ForegroundScale, but the value will be applied just for Windows                                                           |
| IOSForegroundScale     | The same as ForegroundScale, but the value will be applied just for iOS                                                               |
| SkiaForegroundScale    | The same as ForegroundScale, but the value will be applied just for Skia targets                                                               |

> [!NOTE]
> The PlatformsForegroundScale (AndroidForegroundScale, WasmForegroundScale, etc) will override the global ForegroundScale value.

## UnoImage

| Property Name | Description |
| ------------- | ----------- |
| Include       | Used to insert the path of the image asset, could be a `png` or `svg`.             |

## UnoSplashScreen

| Property Name | Description |
| ------------- | ----------- |
| Include       | Used to insert the path of the image asset, could be a `png` or `svg`.             |
| Scale       | Used to scale the image that will be used as SplashScreen. This property will be override by any platform specific scale.            |
| AndroidScale       | Used to scale the image that will be used as SplashScreen on Android platform.            |
| IOSScale       | Used to scale the image that will be used as SplashScreen on iOS platform.            |
| WindowsScale       | Used to scale the image that will be used as SplashScreen on Windows platform.            |
| WasmScale       | Used to scale the image that will be used as SplashScreen on Wasm.            |
| SkiaScale       | Used to scale the image that will be used as SplashScreen on Skia targets (GTK and WPF).            |