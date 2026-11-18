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
	[Collection(TargetPlatformCollection.Name)]
	public class GenerateSplashAssetsTests : MSBuildTaskTestFixture<GenerateSplashAssets_v0>
	{
		protected GenerateSplashAssets_v0 GetNewTask(ITaskItem splash) =>
			new()
			{
				IntermediateOutputPath = DestinationDirectory,
				UnoSplashScreen = new[] { splash },
				BuildEngine = this,
			};

		void AssertFile(string file, int width, int height)
		{
			file = Path.Combine(DestinationDirectory, file);

			Assert.True(File.Exists(file), $"File did not exist: {file}");

			using var codec = SKCodec.Create(file);
			Assert.Equal(width, codec.Info.Width);
			Assert.Equal(height, codec.Info.Height);
		}

		[Theory]
		[InlineData("dotnet_logo", "#512BD4")]
		[InlineData("appiconfg", "#0000FF")]
		public void FileIsGenerated(string image, string color)
		{
			var splash = new TaskItem($"images/{image}.svg", new Dictionary<string, string>
			{
				["BackgroundColor"] = color,
			});

			var task = GetNewTask(splash);
			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			AssertFile($"{image}.scale-100.png", 620, 300);
			AssertFile($"{image}.scale-125.png", 775, 375);
			AssertFile($"{image}.scale-200.png", 1240, 600);
		}

		// https://github.com/unoplatform/uno.resizetizer/issues/293
		[Fact]
		public void UnchangedFilesAreNotRewritten()
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string> { ["BackgroundColor"] = "#0000FF" });
			Assert.True(GetNewTask(splash).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			var files = Directory.GetFiles(DestinationDirectory, "*.png");
			Assert.NotEmpty(files);
			var past = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			foreach (var file in files)
			{
				File.SetLastWriteTimeUtc(file, past);
			}

			Assert.True(GetNewTask(splash).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			Assert.All(files, file => Assert.Equal(past, File.GetLastWriteTimeUtc(file)));

			var recolored = new TaskItem("images/appiconfg.svg", new Dictionary<string, string> { ["BackgroundColor"] = "#FF0000" });
			Assert.True(GetNewTask(recolored).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			Assert.All(files, file => Assert.NotEqual(past, File.GetLastWriteTimeUtc(file)));
		}

		// The star in appiconfg.svg spans this share of the 1024px icon's height, stroke included
		const double StarHeightRatio = (792.633406 - 164 + 9) / 1024;

		[Theory]
		[InlineData(null, null, 1.0)]
		[InlineData("0.5", null, 0.5)]
		[InlineData(null, "0.4", 0.4)]
		[InlineData("0.8", "0.4", 0.4)]
		public void ScaleShrinksTheArt(string scale, string windowsScale, double expected)
		{
			var metadata = new Dictionary<string, string> { ["BackgroundColor"] = "#0000FF" };
			AddIfSet(metadata, "Scale", scale);
			AddIfSet(metadata, "WindowsScale", windowsScale);

			Assert.True(GetNewTask(new TaskItem("images/appiconfg.svg", metadata)).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			AssertArtHeight("appiconfg.scale-100.png", 300 * StarHeightRatio * expected);
			AssertArtHeight("appiconfg.scale-200.png", 600 * StarHeightRatio * expected);
		}

		// The platform scales are read for the platform that last ran ResizetizeImages, which runs after
		// the splash screen and can be another project's in the same build node
		[Theory]
		[InlineData(null)]
		[InlineData("android")]
		[InlineData("wasm")]
		[InlineData("uwp")]
		public void WindowsScaleIsUsedWhateverPlatformRanBefore(string previousPlatform)
		{
			RunResizetizeImages(previousPlatform);

			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
			{
				["BackgroundColor"] = "#0000FF",
				["AndroidScale"] = "0.3",
				["WasmScale"] = "0.3",
				["SkiaScale"] = "0.3",
				["WindowsScale"] = "0.6",
			});

			Assert.True(GetNewTask(splash).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			AssertArtHeight("appiconfg.scale-100.png", 300 * StarHeightRatio * 0.6);
		}

		[Fact]
		public void BackgroundColorFillsTheCanvasWhenScaled()
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
			{
				["BackgroundColor"] = "#0000FF",
				["Scale"] = "0.3",
			});

			Assert.True(GetNewTask(splash).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			using var bitmap = SKBitmap.Decode(Path.Combine(DestinationDirectory, "appiconfg.scale-100.png"));
			Assert.Equal(new SKColor(0, 0, 255), bitmap.GetPixel(0, 0));
			Assert.Equal(new SKColor(0, 0, 255), bitmap.GetPixel(310, 20));
			Assert.Equal(new SKColor(0, 0, 255), bitmap.GetPixel(619, 299));
		}

		static void AddIfSet(Dictionary<string, string> metadata, string name, string value)
		{
			if (value is not null)
			{
				metadata[name] = value;
			}
		}

		void RunResizetizeImages(string platformType)
		{
			var task = new ResizetizeImages_v0
			{
				PlatformType = platformType,
				IntermediateOutputPath = Path.Combine(DestinationDirectory, "resizetized"),
				IntermediateOutputIconPath = Path.Combine(DestinationDirectory, "resizetized"),
				InputsFile = new[] { "unoimage.inputs" },
				Images = Array.Empty<ITaskItem>(),
				BuildEngine = this,
			};

			Assert.True(task.Execute(), LogErrorEvents.FirstOrDefault()?.Message);
		}

		// Measures the rows holding anything other than the blue background
		void AssertArtHeight(string file, double expected)
		{
			using var bitmap = SKBitmap.Decode(Path.Combine(DestinationDirectory, file));

			int top = -1, bottom = -1;
			for (var y = 0; y < bitmap.Height; y++)
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if (pixel.Red > 40 || pixel.Green > 40 || pixel.Blue < 215)
					{
						top = top < 0 ? y : top;
						bottom = y;
						break;
					}
				}
			}

			Assert.True(top >= 0, $"{file} has no art");
			// Antialiasing and the stroke add a few pixels
			var tolerance = Math.Max(3, expected * 0.02);
			Assert.InRange(bottom - top + 1, expected - tolerance, expected + tolerance);
		}

		[Theory(Skip = "We don't worked on SplashScreen")]
		[InlineData(null, "appiconfg")]
		[InlineData("images/CustomAlias.svg", "CustomAlias")]
		public void SplashScreenResectsAlias(string alias, string outputImage)
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
			{
				["Link"] = alias,
			});

			var task = GetNewTask(splash);
			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			AssertFile($"{outputImage}SplashScreen.scale-100.png", 620, 300);
			AssertFile($"{outputImage}SplashScreen.scale-125.png", 775, 375);
			AssertFile($"{outputImage}SplashScreen.scale-200.png", 1240, 600);
		}
	}
}
