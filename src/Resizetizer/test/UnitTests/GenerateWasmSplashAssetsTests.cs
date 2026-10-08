using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SkiaSharp;
using Xunit;

namespace Uno.Resizetizer.Tests
{
	public class GenerateWasmSplashAssetsTests : MSBuildTaskTestFixture<GenerateWasmSplashAssets_v0>
	{
		const string UserManifest = "var UnoAppManifest = {\n    splashScreenImage: \"old.png\",\n    splashScreenColor: \"#123456\",\n    displayName: \"MyApp\",\n}";

		string OutputFile => Path.Combine(DestinationDirectory, "out", "UnoAppManifest.js");

		string Run(Dictionary<string, string> metadata, string userManifest = UserManifest, string image = "images/dotnet_logo.svg")
		{
			Directory.CreateDirectory(DestinationDirectory);
			var manifestPath = Path.Combine(DestinationDirectory, "AppManifest.js");
			File.WriteAllText(manifestPath, userManifest);

			var task = new GenerateWasmSplashAssets_v0
			{
				IntermediateOutputPath = DestinationDirectory,
				OutputFile = OutputFile,
				UnoSplashScreen = new ITaskItem[] { new TaskItem(image, metadata) },
				EmbeddedResources = new ITaskItem[] { new TaskItem(manifestPath) },
				BuildEngine = this,
			};

			Assert.True(task.Execute(), LogErrorEvents.FirstOrDefault()?.Message);
			return File.ReadAllText(OutputFile);
		}

		static string Manifest(params string[] lines) =>
			"var UnoAppManifest = {" + Environment.NewLine
			+ string.Concat(lines.Select(l => "    " + l + "," + Environment.NewLine))
			+ "}";

		[Fact]
		public void ManifestIsUnchangedWithoutDarkMetadata()
		{
			var result = Run(new() { ["Color"] = "#512BD4" });

			Assert.Equal(
				Manifest(
					"splashScreenImage: \"dotnet_logo.scale-200.png\"",
					"splashScreenColor: \"#512bd4\"",
					"displayName: \"MyApp\""),
				result);
		}

		[Fact]
		public void DarkColorAddsThemeBackgrounds()
		{
			var result = Run(new() { ["Color"] = "#FFFFFF", ["DarkColor"] = "#101010" });

			Assert.Equal(
				Manifest(
					"splashScreenImage: \"dotnet_logo.scale-200.png\"",
					"splashScreenColor: \"#ffffff\"",
					"displayName: \"MyApp\"",
					"lightThemeBackgroundColor: \"#ffffff\"",
					"darkThemeBackgroundColor: \"#101010\""),
				result);
		}

		[Fact]
		public void DarkColorWithoutColorOnlyWritesDarkBackground()
		{
			var result = Run(new() { ["DarkColor"] = "#101010" });

			Assert.DoesNotContain("lightThemeBackgroundColor", result);
			Assert.Contains("darkThemeBackgroundColor: \"#101010\"", result);
			Assert.Contains("splashScreenColor: \"transparent\"", result);
		}

		[Fact]
		public void DarkColorAlphaIsStripped()
		{
			var result = Run(new() { ["Color"] = "#80FFFFFF", ["DarkColor"] = "#80101010" });

			Assert.Contains("lightThemeBackgroundColor: \"#ffffff\"", result);
			Assert.Contains("darkThemeBackgroundColor: \"#101010\"", result);
		}

		[Fact]
		public void DarkImageAddsDarkSplashImage()
		{
			var result = Run(new() { ["Color"] = "#FFFFFF", ["DarkImage"] = "images/appiconfg.svg" });

			Assert.Equal(
				Manifest(
					"splashScreenImage: \"dotnet_logo.scale-200.png\"",
					"splashScreenColor: \"#ffffff\"",
					"displayName: \"MyApp\"",
					"splashScreenImageDark: \"appiconfg.scale-200.png\""),
				result);
		}

		[Fact]
		public void DarkImageAndColorWriteAllKeys()
		{
			var result = Run(new()
			{
				["Color"] = "#FFFFFF",
				["DarkColor"] = "#101010",
				["DarkImage"] = "images/appiconfg.svg",
			});

			Assert.Equal(
				Manifest(
					"splashScreenImage: \"dotnet_logo.scale-200.png\"",
					"splashScreenColor: \"#ffffff\"",
					"displayName: \"MyApp\"",
					"lightThemeBackgroundColor: \"#ffffff\"",
					"darkThemeBackgroundColor: \"#101010\"",
					"splashScreenImageDark: \"appiconfg.scale-200.png\""),
				result);
		}

		[Fact]
		public void UserProvidedKeysArePreserved()
		{
			var result = Run(
				new() { ["Color"] = "#FFFFFF", ["DarkColor"] = "#101010" },
				"var UnoAppManifest = {\n    displayName: \"MyApp\",\n    lightThemeBackgroundColor: \"#ABCDEF\",\n    custom: \"x\",\n}");

			Assert.Contains("displayName: \"MyApp\"", result);
			Assert.Contains("custom: \"x\"", result);
		}

		[Fact]
		public void InvalidDarkColorThrows() =>
			Assert.Throws<InvalidDataException>(() => Run(new() { ["DarkColor"] = "not-a-color" }));

		[Fact]
		public void MissingDarkImageThrows() =>
			Assert.Throws<FileNotFoundException>(() => Run(new() { ["DarkImage"] = "images/nope.svg" }));

		[Theory]
		[InlineData("dotnet_logo", "appiconfg")]
		public void DarkSplashImageIsGeneratedLikeTheLightOne(string light, string dark)
		{
			var metadata = new Dictionary<string, string>
			{
				["BaseSize"] = "100,100",
				["Resize"] = "true",
				["IsSplashScreen"] = "true",
				["Link"] = "",
			};

			var task = new ResizetizeImages_v0
			{
				TargetFramework = "wasm",
				PlatformType = "wasm",
				IntermediateOutputPath = DestinationDirectory,
				IntermediateOutputIconPath = DestinationDirectory,
				InputsFile = new[] { "unoimage.inputs", "unosplash.inputs" },
				Images = new ITaskItem[]
				{
					new TaskItem($"images/{light}.svg", metadata),
					new TaskItem($"images/{dark}.svg", metadata),
				},
				BuildEngine = this,
			};

			Assert.True(task.Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			AssertFileSize($"{light}.scale-200.png", 200, 200);
			AssertFileSize($"{dark}.scale-200.png", 200, 200);
		}
	}
}
