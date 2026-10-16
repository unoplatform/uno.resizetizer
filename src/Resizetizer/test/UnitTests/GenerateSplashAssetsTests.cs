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
