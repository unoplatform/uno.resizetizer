

---
uid: Overview.Uno.Resizetizer
---

# How-To: Get Started with Uno.Resizetizer

Uno.Resizetizer is a set of MSBuild tasks that will manage the assets of your application for you. With it you don't need to care about to generate and maintain different images sizes and all the work to create a splash screen, you just need to provide a `svg` file and the tool will take care of the rest for you.

This tutorial will walk through how to use it on your Uno application. If you need help creating a new Uno app please see this doc.

# Installation

The Uno.Resizeter is delivered by Nuget, so in order to install it you can either download it using your IDE (this will be showed in the next steps) or added directly on your `csproj` as showed in the Nuget [page](https://www.nuget.org/packages/Uno.Resizetizer/).

### 1. Installing Uno.Resizetizer

* Open your favorite IDE, in this case will be Visual Studio, after that open the Manage Nuget packages window
* Search for `Uno.Resizetizer` and install it over your projects

# Usage

Uno.Resizetizer can handle:

* Images used in the application
* App icon
* Splash screen

We will show how to use it for each case in the next sections

### UnoImage

`UnoImage` is the build action used for images that you want to show on your app.

### 2. Configure your project to use generated Images

* On your shared project, create a folder called `Assets` (if doesn't exist) and then create a folder called `Images`, now you just need to add your assets to this folder.
> Those folders are just for the sample, you create with any name that you want and how many levels do you need.
> Make sure the build assets are configured to be `UnoImage`. If you want you can add this line on your `csproj` to make all files inside the `Assets\Images` folder to be automatically configured to be `UnoImage`.

```xml
<ItemGroup>
	<UnoImage Include="Assets\Images\*" />
</ItemGroup>
```

### 3. Using the assets on your project

* Now you can reference it on your project just like a regular image. For example:
```xml
<Image Width="300"
Height="300"
Source="Assets\\Images\\myImage.png" />
```
> 
> Remeber to add the `.png` at the end of name file. 

# UnoIcon

`UnoIcon` is the build action for the app icon. It should have just one per application. The `UnoIcon` accepts two assets, one that represents the Foreground and another that represents the Background. During the generation phase those files will be merged into one `png` image.

### 4. Configuring your project to use generated app icon

* Create a `Icon` folder inside the Base project, and add the files related to app icon there.
* Now open the `base.props` file, sinde the Base folder project and add this to the `ItemGroup` property
```xml
<ItemGroup>
	<UnoIcon Include="$(MSBuildThisFileDirectory)Icons\iconapp.svg"
			 ForegroundFile="$(MSBuildThisFileDirectory)Icons\appconfig.svg"
			 Color="#FF0000"
</ItemGroup>
```

Now we need to do some adjustments on `Android`, `windows`, `mac-catalyst` and `iOS`. Let's start with `Android`.

* Open the `Main.Android.cs` file (or the file that has the `Android.App.ApplicationAttribute`), and change the `Icon` property, in that attribute, to be the name of the file used in the `Include` property of `UnoIcon`, in our case will be:

```csharp
[global::Android.App.ApplicationAttribute(
Label = "@string/ApplicationName",
Icon = "@mipmap/iconapp",
//...
)]
```


> Feel free to delete the old assets related to app icon on `Android` project

Now let's jump to Windows platform.

- Open the `Package.appxmanifest` file and look for the `Application` tag


Now let's jump to the Apple's platform.

* For `mac-catalyst` and `iOS`, open the `info.plist` file and find for the `XSAppIconAsset` key, change its value to be `Assets.xcassets/iconapp.appiconset`. 

> If your app icon has other name than `iconapp` use it instead.

> Feel free to delete the old assets related to app icon in the project.

