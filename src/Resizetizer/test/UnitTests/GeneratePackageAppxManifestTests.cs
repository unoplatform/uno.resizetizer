#nullable enable
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Uno.Resizetizer.Tests
{
	public class GeneratePackageAppxManifestTests : MSBuildTaskTestFixture<GeneratePackageAppxManifest_v0>
	{
		protected GeneratePackageAppxManifest_v0 GetNewTask(
			string manifest,
			string? generatedFilename = null,
			string? guid = null,
			string? displayVersion = null,
			string? version = null,
			string? displayName = null,
			ITaskItem? appIcon = null,
			ITaskItem? splashScreen = null)
		{
			return new()
			{
				IntermediateOutputPath = DestinationDirectory,
				BuildEngine = this,
				GeneratedFilename = generatedFilename,
				AppxManifest = new []{new TaskItem(manifest)},
				ApplicationId = guid,
				ApplicationDisplayVersion = displayVersion,
				ApplicationVersion = version,
				ApplicationTitle = displayName,AppIcon = appIcon == null ? null : new[] { appIcon },
				SplashScreen = splashScreen == null ? null : new[] { splashScreen },
				TargetFramework = "windows"
			};
		}

		protected GeneratePackageAppxManifest_v0 GetNewTask(
		ITaskItem[] appxManifests,
		string? generatedFilename = null,
		string? guid = null,
		string? displayVersion = null,
		string? version = null,
		string? displayName = null,
		ITaskItem? appIcon = null,
		ITaskItem? splashScreen = null)
		{
			return new()
			{
				IntermediateOutputPath = DestinationDirectory,
				BuildEngine = this,
				GeneratedFilename = generatedFilename,
				AppxManifest = appxManifests,
				ApplicationId = guid,
				ApplicationDisplayVersion = displayVersion,
				ApplicationVersion = version,
				ApplicationTitle = displayName,AppIcon = appIcon == null ? null : new[] { appIcon },
				SplashScreen = splashScreen == null ? null : new[] { splashScreen },
				TargetFramework = "windows"
			};
		}

		[Theory]
		[InlineData(null, "Package.appxmanifest")]
		[InlineData("GenPkg.appxmanifest", "GenPkg.appxmanifest")]
		public void FileIsGenerated(string? specificFn, string outputFn)
		{
			var task = GetNewTask($"testdata/appxmanifest/typical.appxmanifest", generatedFilename: specificFn);

			var success = task.Execute();

			Assert.True(success, $"{task.GetType()}.Execute() failed: " + LogErrorEvents.FirstOrDefault()?.Message);

			Assert.True(File.Exists(Path.Combine(DestinationDirectory, outputFn)), "Package.appxmanifest file was not generated.");
		}

		[Fact]
		public void ManifestTakesPriority()
		{
			var appIcon = new TaskItem("images/camera.svg");
			appIcon.SetMetadata("ForegroundFile", "images/loginbg.png");
			appIcon.SetMetadata("IsAppIcon", "true");

			var splashScreen = new TaskItem("images/dotnet_logo.svg");
			splashScreen.SetMetadata("Color", "#FFFFFF");

			var inputFilename = $"testdata/appxmanifest/typical.appxmanifest";
			var task = GetNewTask(inputFilename,
				guid: "3505f9e4-fa3e-4742-d1ac-daff4ec89b2b",
				displayVersion: "2.5",
				version: "3",
				displayName: "Fishy Things",
				appIcon: appIcon,
				splashScreen: splashScreen);

			var success = task.Execute();
			Assert.True(success, $"{task.GetType()}.Execute() failed: " + LogErrorEvents.FirstOrDefault()?.Message);

			var outputFilename = Path.Combine(DestinationDirectory, "Package.appxmanifest");
			var expectedFilename = $"testdata/appxmanifest/typical.appxmanifest";

			var outputDoc = XDocument.Load(outputFilename);
			var expectedDoc = XDocument.Load(expectedFilename);

			if (!XNode.DeepEquals(outputDoc, expectedDoc))
			{
				Assert.Equal(expectedDoc.ToString(), outputDoc.ToString());
			}
		}

		[Fact]
		public void CorrectGenerationWhenUserSpecifyBackgroundColor()
		{
			var input = "empty";
			var expected = "typicalWithNoBackground";
			var appIcon = new TaskItem("images\\appicon.svg");
			appIcon.SetMetadata("ForegroundFile", "images\\appiconfg.svg");
			appIcon.SetMetadata("Link", "images\\appicon.svg");
			appIcon.SetMetadata("IsAppIcon", "true");

			var splashScreen = new TaskItem("images/dotnet_bot.svg");
			splashScreen.SetMetadata("Color", "#FFFFFF");
			appIcon.SetMetadata("Color", "#FFFFFF");

			var inputFilename = $"testdata/appxmanifest/{input}.appxmanifest";
			var task = GetNewTask(inputFilename,
				guid: "f9e4fa3e-3505-4742-9b2b-d1acdaff4ec8",
				displayVersion: "1.0.0",
				version: "1",
				displayName: "Sample App",
				appIcon: appIcon,
				splashScreen: splashScreen);



			var success = task.Execute();
			Assert.True(success, $"{task.GetType()}.Execute() failed: " + LogErrorEvents.FirstOrDefault()?.Message);

			var outputFilename = Path.Combine(DestinationDirectory, "Package.appxmanifest");
			var expectedFilename = $"testdata/appxmanifest/{expected}.appxmanifest";

			var outputDoc = XDocument.Load(outputFilename);
			var expectedDoc = XDocument.Load(expectedFilename);

			if (!XNode.DeepEquals(outputDoc, expectedDoc))
			{
				Assert.Equal(expectedDoc.ToString(), outputDoc.ToString());
			}
		}

		[Fact]
		public void CorrectGenerationWhitOutBackgroundColor()
		{
			var input = "typical";
			var expected = "typical";
			var appIcon = new TaskItem("images/appicon.svg");
			appIcon.SetMetadata("ForegroundFile", "images/appiconfg.svg");
			appIcon.SetMetadata("IsAppIcon", "true");

			var splashScreen = new TaskItem("images/dotnet_bot.svg");
			splashScreen.SetMetadata("Color", "#FFFFFF");

			var inputFilename = $"testdata/appxmanifest/{input}.appxmanifest";
			var task = GetNewTask(inputFilename,
				guid: "f9e4fa3e-3505-4742-9b2b-d1acdaff4ec8",
				displayVersion: "1.0.0",
				version: "1",
				displayName: "Sample App",
				appIcon: appIcon,
				splashScreen: splashScreen);

			var success = task.Execute();
			Assert.True(success, $"{task.GetType()}.Execute() failed: " + LogErrorEvents.FirstOrDefault()?.Message);

			var outputFilename = Path.Combine(DestinationDirectory, "Package.appxmanifest");
			var expectedFilename = $"testdata/appxmanifest/{expected}.appxmanifest";

			var outputDoc = XDocument.Load(outputFilename);
			var expectedDoc = XDocument.Load(expectedFilename);

			if (!XNode.DeepEquals(outputDoc, expectedDoc))
			{
				Assert.Equal(expectedDoc.ToString(), outputDoc.ToString());
			}
		}

		[Theory]
		[InlineData("2", "42", "2.0.0.42")]
		[InlineData("2.1", "42", "2.1.0.42")]
		[InlineData("3.2.1", "42", "3.2.1.42")]
		[InlineData("4.3.2.1", "", "4.3.2.1")]
		public void ValidMergeVersionNumbers(string displayVersion, string appVersion, string expectedResult)
		{
			var result = GeneratePackageAppxManifest_v0.TryMergeVersionNumbers(displayVersion, appVersion, out var merged);
			Assert.True(result);
			Assert.Equal(expectedResult, merged);
		}

		[Theory]
		[InlineData("2.1", "42.31")]
		[InlineData("4.3.2.1", "42")]
		[InlineData("1.0.0", "1.0.0")]
		[InlineData("3.1.3a1", "42")]
		[InlineData("6.0-preview.7", "42")]
		public void InvalidMergeVersionNumbers(string displayVersion, string appVersion)
		{
			var result = GeneratePackageAppxManifest_v0.TryMergeVersionNumbers(displayVersion, appVersion, out var merged);
			Assert.False(result);
		}

		[Fact]
		public void TaskShouldFileAWarningIfMoreThanOneManifestIsProvided()
		{
			// Arrange
			var taskItem = new TaskItem("testdata/appxmanifest/typical.appxmanifest");
			var task = GetNewTask(appxManifests: new [] {taskItem, taskItem});

			// Act
			task.Execute();

			// Assert
			Assert.True(LogWarningEvents.Count > 0, "Warnings should be greater than zero");
		}
	}
}
