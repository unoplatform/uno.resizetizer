---
uid: Overview.Uno.Resizetizer
---

# How-To: Get Started with Uno.Resizetizer

Uno.Resizetizer is a set of MSBuild tasks to manage the assets of an app. Using this package, it is not needed to care about generating and maintaining different image sizes/scaling and create a splash screen plumbing. It is only needed to provide an `svg` file and the tool will take care of the rest.

This tutorial will walk through how to use it on your Uno Platform app. To create an app, make sure to visit [our getting started tutorials](xref:Uno.GetStarted).

## Installation

Uno.Resizeter is delivered [through NuGet](https://www.nuget.org/packages/Uno.Resizetizer). In order to install it you can either download it using your IDE (this will be showed in the next steps) or added directly on your `.csproj` as showed in the [NuGet page](https://www.nuget.org/packages/Uno.Resizetizer/).

### 1. Installing Uno.Resizetizer

* Open your favorite IDE, in this case it will be Visual Studio, after that open the Manage NuGet packages window
* Search for `Uno.Resizetizer` and install it over your projects

> [!NOTE]
> Uno.Resizetizer is compatible with projects running .NET 6 and later.

## Usage

Uno.Resizetizer can handle:

* Images used in the application
* The App icon
* The splash screen

The next sections will show how to use it for each use case.

### UnoImage

`UnoImage` is the build action used for images that will be part of the app.

### 2. Configure the project to use generated Images

* In the App Class library, create a folder called `Assets` (if doesn't exist) and then create a folder called `Images`. We now need to add assets to this folder.

> [!TIP]
> Those folders names are examples. It is possible to create folders with any name and how many levels needed.

Make sure that the build assets are configured to be `UnoImage`. In the `csproj`, to make all files inside the `Assets\Images` folder to be automatically configured to be `UnoImage`, add the following:

```xml
<ItemGroup>
    <UnoImage Include="Assets\Images\*" />
</ItemGroup>
```

### 3. Using the assets on the project

* `UnoImage` assets can now be used just like any regular image. For example:

```xml
<Image Width="300"
       Height="300"
       Source="Assets\Images\myImage.png" />
```

> [!TIP]
> Make sure to add the `.png` at the end of the file name

## UnoIcon

`UnoIcon` is the build action for the app icon. There should only be one per application. The `UnoIcon` accepts two assets, one that represents the `Foreground` and another that represents the `Background`. During the generation phase, those files will be merged into one `.png` image.

### 4. Configuring the project to use generated app icon

* Create a `Icon` folder inside the Base project, and add the files related to app icon there.
* Now open the `base.props` file, inside the `MyApp.Base` folder project and add the following block

```xml
<ItemGroup>
    <UnoIcon Include="$(MSBuildThisFileDirectory)Icons\iconapp.svg"
             ForegroundFile="$(MSBuildThisFileDirectory)Icons\appconfig.svg"
             Color="#FF0000"/>
</ItemGroup>
```

Next, some adjustments are needed on `Android`, `Windows`, `mac-catalyst` and `iOS`. Let's start with `Android`.

* Open the `Main.Android.cs` file (or the file that has the `Android.App.ApplicationAttribute`), and change the `Icon` property, in that attribute, to be the name of the file used in the `Include` property of `UnoIcon`, in our case will be:

```csharp
[global::Android.App.ApplicationAttribute(
Label = "@string/ApplicationName",
Icon = "@mipmap/iconapp",
//...
)]
```

> [!TIP]
> Feel free to remove the old assets related to app icon on `Android` project

Now let's jump to Windows platform.

* Open the `Package.appxmanifest` file and look for the `Application` tag
* And remove everything that's related to application icon. It should look like this:

```xml
<Applications>
   <Application Id="App"
     Executable="$targetnametoken$.exe"
     EntryPoint="$targetentrypoint$">
     <uap:VisualElements
       DisplayName="Resizetizer.Extensions.Sample"
       Description="Resizetizer.Extensions.Sample">
       <uap:SplashScreen Image="Resizetizer.Extensions.Sample/Assets/SplashScreen.png" />
       <uap:DefaultTile/>
     </uap:VisualElements>
   </Application>
 </Applications>
```

Now let's jump to the Apple's platform.

* For `mac-catalyst` and `iOS`, open the `info.plist` file and find for the `XSAppIconAsset` key, change its value to be `Assets.xcassets/iconapp.appiconset`.

> [!NOTE]
> If your app icon has other name than `iconapp` use it instead.

> [!TIP]
> Feel free to delete the old assets related to app icon in the project.

## UnoSplashScreen

`UnoSplashScreen` is the build action for the splash screen. There should only be one per application. The `UnoSplashScreen` has two more properties that you can use to adjust your asset, they are:

* BaseSize: It's the size that will be used to perform the scaling of the image. The default value is the size of asset. So if you feel that your SplashScreen doesn't look right you can tweak this value.

* Color: It's the background color of that will be used to fill the empty space on the final SplashScreen asset. The default value is `#FFFFFF`(transparent).

### 5. Configuring the project to use generated splash screen

* Create a `SplashScreen` folder inside the Base project, and add the file related to splash screen there.
* Now open the `base.props` file, inside the `MyApp.Base` folder project and add the following block

```xml
<UnoSplashScreen
         Include="$(MSBuildThisFileDirectory)Splash\splash_screen.svg"
         BaseSize="128,128"
         Color="#512BD4" />
```

Next some adjustments are needed on `Android`, `Windows`, `wasm`, `mac-catalyst` and `iOS`. Let's start with `Android`.

