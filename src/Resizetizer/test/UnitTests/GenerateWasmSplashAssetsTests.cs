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
		public void DarkBackgroundColorAddsThemeBackgrounds()
		{
			var result = Run(new() { ["Color"] = "#FFFFFF", ["DarkBackgroundColor"] = "#101010" });

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
		public void DarkBackgroundColorWithoutColorOnlyWritesDarkBackground()
		{
			var result = Run(new() { ["DarkBackgroundColor"] = "#101010" }, "var UnoAppManifest = {\n    displayName: \"MyApp\",\n}");

			Assert.DoesNotContain("lightThemeBackgroundColor", result);
			Assert.Contains("darkThemeBackgroundColor: \"#101010\"", result);
			Assert.DoesNotContain("splashScreenColor", result);
		}

		[Fact]
		public void DarkBackgroundColorAlphaIsStripped()
		{
			var result = Run(new() { ["Color"] = "#80FFFFFF", ["DarkBackgroundColor"] = "#80101010" });

			Assert.Contains("lightThemeBackgroundColor: \"#ffffff\"", result);
			Assert.Contains("darkThemeBackgroundColor: \"#101010\"", result);
		}

		[Fact]
		public void DarkFileAddsDarkSplashImage()
		{
			var result = Run(new() { ["Color"] = "#FFFFFF", ["DarkFile"] = "images/appiconfg.svg" });

			Assert.Equal(
				Manifest(
					"splashScreenImage: \"dotnet_logo.scale-200.png\"",
					"splashScreenColor: \"#ffffff\"",
					"displayName: \"MyApp\"",
					"splashScreenImageDark: \"appiconfg.scale-200.png\""),
				result);
		}

		[Fact]
		public void DarkFileAndColorWriteAllKeys()
		{
			var result = Run(new()
			{
				["Color"] = "#FFFFFF",
				["DarkBackgroundColor"] = "#101010",
				["DarkFile"] = "images/appiconfg.svg",
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
				new() { ["Color"] = "#FFFFFF", ["DarkBackgroundColor"] = "#101010" },
				"var UnoAppManifest = {\n    displayName: \"MyApp\",\n    lightThemeBackgroundColor: \"#ABCDEF\",\n    custom: \"x\",\n}");

			Assert.Contains("displayName: \"MyApp\"", result);
			Assert.Contains("custom: \"x\"", result);
		}

		[Fact]
		public void BackgroundColorBehavesLikeColor()
		{
			var color = Run(new() { ["Color"] = "#512BD4" });
			var background = Run(new() { ["BackgroundColor"] = "#512BD4" });

			Assert.Equal(color, background);
		}

		[Fact]
		public void BackgroundColorWinsOverColor()
		{
			var result = Run(new() { ["Color"] = "#111111", ["BackgroundColor"] = "#512BD4" });

			Assert.Contains("splashScreenColor: \"#512bd4\"", result);
			Assert.DoesNotContain("#111111", result);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("transparent")]
		[InlineData("Transparent")]
		[InlineData("#00000000")]
		[InlineData("#00FFFFFF")]
		public void UnsetBackgroundWritesNoColorKeys(string? background)
		{
			var metadata = new Dictionary<string, string>();
			if (background is not null)
			{
				metadata["BackgroundColor"] = background;
			}

			var result = Run(metadata, "var UnoAppManifest = {\n    displayName: \"MyApp\",\n}");

			Assert.Equal(
				Manifest(
					"displayName: \"MyApp\"",
					"splashScreenImage: \"dotnet_logo.scale-200.png\""),
				result);
		}

		[Fact]
		public void TransparentDarkBackgroundIsTreatedAsUnset()
		{
			var result = Run(new() { ["BackgroundColor"] = "#FFFFFF", ["DarkBackgroundColor"] = "#00000000" });

			Assert.DoesNotContain("darkThemeBackgroundColor", result);
			Assert.DoesNotContain("lightThemeBackgroundColor", result);
			Assert.Contains("splashScreenColor: \"#ffffff\"", result);
		}

		[Fact]
		public void DarkBackgroundWithUnsetBackgroundOnlyWritesDarkKey()
		{
			var result = Run(new() { ["DarkBackgroundColor"] = "#101010" }, "var UnoAppManifest = {\n    displayName: \"MyApp\",\n}");

			Assert.Equal(
				Manifest(
					"displayName: \"MyApp\"",
					"splashScreenImage: \"dotnet_logo.scale-200.png\"",
					"darkThemeBackgroundColor: \"#101010\""),
				result);
		}

		[Fact]
		public void DarkBackgroundWithBackgroundColorWritesBothThemes()
		{
			var result = Run(new() { ["BackgroundColor"] = "#FFFFFF", ["DarkBackgroundColor"] = "#101010" });

			Assert.Contains("splashScreenColor: \"#ffffff\"", result);
			Assert.Contains("lightThemeBackgroundColor: \"#ffffff\"", result);
			Assert.Contains("darkThemeBackgroundColor: \"#101010\"", result);
		}

		[Fact]
		public void InvalidDarkBackgroundColorThrows() =>
			Assert.Throws<InvalidDataException>(() => Run(new() { ["DarkBackgroundColor"] = "not-a-color" }));

		[Fact]
		public void BackgroundColorOnItemParsesIntoColor()
		{
			var item = new TaskItem("images/dotnet_logo.svg", new Dictionary<string, string> { ["BackgroundColor"] = "#512BD4" });
			var info = ResizeImageInfo.Parse(item);

			Assert.Equal(SKColor.Parse("#512BD4"), info.Color);
		}

		[Fact]
		public void MissingDarkFileThrows() =>
			Assert.Throws<FileNotFoundException>(() => Run(new() { ["DarkFile"] = "images/nope.svg" }));

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
