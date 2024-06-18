#nullable enable
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Resizetizer.UnitTests.Mocks;
using System;
using System.Collections;
using System.Collections.Generic;
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
			string? applicationId = null,
			string? displayVersion = null,
			string? version = null,
			string? displayName = null,
			string? description = null,
			string? applicationPublisher = null,
			ITaskItem? appIcon = null,
			ITaskItem? splashScreen = null)
		{
			return new()
			{
				IntermediateOutputPath = DestinationDirectory,
				BuildEngine = this,
				GeneratedFilename = generatedFilename,
				AppxManifest = [new TaskItem(manifest)],
				ApplicationId = applicationId,
				ApplicationDisplayVersion = displayVersion,
				ApplicationVersion = version,
				ApplicationTitle = displayName,
				AssemblyName = GetType().Assembly.FullName,
				Description = description,
				ApplicationPublisher = applicationPublisher,
				AppIcon = appIcon == null ? null : [appIcon],
				SplashScreen = splashScreen == null ? null : [splashScreen],
				TargetFramework = "windows"
			};
		}

		protected GeneratePackageAppxManifest_v0 GetNewTask(
		ITaskItem[] appxManifests,
		string? generatedFilename = null,
		string? applicationId = null,
		string? displayVersion = null,
		string? version = null,
		string? displayName = null,
		string? description = null,
		string? applicationPublisher = null,
		ITaskItem? appIcon = null,
		ITaskItem? splashScreen = null,
		string? targetPlatformVersion = null,
		string? targetPlatformMinVersion = null)
		{
			return new()
			{
				IntermediateOutputPath = DestinationDirectory,
				BuildEngine = this,
				GeneratedFilename = generatedFilename,
				AppxManifest = appxManifests,
				ApplicationId = applicationId,
				ApplicationDisplayVersion = displayVersion,
				ApplicationVersion = version,
				ApplicationTitle = displayName,
				Description = description,
				ApplicationPublisher = applicationPublisher,
				AssemblyName = GetType().Assembly.GetName().Name,
				AppIcon = appIcon == null ? null : [appIcon],
				SplashScreen = splashScreen == null ? null : [splashScreen],
				TargetFramework = "windows",
				TargetPlatformVersion = targetPlatformVersion,
				TargetPlatformMinVersion = targetPlatformMinVersion
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
			var appIcon = MockTaskItem.CreateAppIcon("images/camera.svg", "images/loginbg.png");
			var splashScreen = MockTaskItem.CreateSplashScreen("images/dotnet_logo.svg", "#FFFFFF");

			var inputFilename = $"testdata/appxmanifest/typical.appxmanifest";
			var task = GetNewTask(inputFilename,
				applicationId: "com.contoso.myapp",
				displayVersion: "2.5",
				version: "3",
				displayName: "Fishy Things",
				appIcon: appIcon,
				splashScreen: splashScreen);

			var success = task.Execute();
			Assert.True(success, $"{task.GetType()}.Execute() failed: " + LogErrorEvents.FirstOrDefault()?.Message);

			var outputFilename = Path.Combine(DestinationDirectory, "Package.appxmanifest");
			var expectedFilename = $"testdata/appxmanifest/typical.expected.appxmanifest";

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
			var appIcon = MockTaskItem.CreateAppIcon("images/appicon.svg", "images/appiconfg.svg");
			appIcon.SetMetadata("Color", "#FFFFFF");
			var splashScreen = MockTaskItem.CreateSplashScreen("images/dotnet_bot.svg", "#FFFFFF");

			var inputFilename = $"testdata/appxmanifest/{input}.appxmanifest";
			var task = GetNewTask(inputFilename,
				applicationId: "com.contoso.myapp",
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

			var appIcon = MockTaskItem.CreateAppIcon("images/appicon.svg", "images/appiconfg.svg");
			var splashScreen = MockTaskItem.CreateSplashScreen("images/dotnet_bot.svg", "#FFFFFF");

			var inputFilename = $"testdata/appxmanifest/{input}.appxmanifest";
			var task = GetNewTask(inputFilename,
				applicationId: "com.contoso.myapp",
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

		[Fact]
		public void TaskShouldNotDuplicateTargetVersions()
		{
			// Arrange
			var taskItem = new TaskItem("testdata/appxmanifest/duplicate-versions.input.appxmanifest");
			var task = GetNewTask(
				appxManifests: [taskItem],
				targetPlatformVersion: "10.0.19041.0",
				targetPlatformMinVersion: "10.0.17763.0");

			// Act
			task.Execute();

			// Assert
			AssertExpectedManifest(task.GeneratedAppxManifest.ItemSpec, taskItem.ItemSpec);
		}

		[Theory]
		[InlineData("template")]
		public void GeneratesExpectedAppxManifest(string manifestName)
		{
			// Arrange
			var appIcon = MockTaskItem.CreateAppIcon("images/camera.svg", "images/loginbg.png");
			var splashScreen = MockTaskItem.CreateSplashScreen("images/dotnet_logo.svg", "#FFFFFF");

			var taskItem = new TaskItem($"testdata/appxmanifest/{manifestName}.input.appxmanifest");
			var task = GetNewTask(
				appxManifests: [taskItem],
				applicationId: "com.contoso.myapp",
				displayVersion: "1.0.0",
				version: "1",
				displayName: "Sample App",
				description: "This is a sample from the Unit Tests",
				appIcon: appIcon,
				splashScreen: splashScreen,
				targetPlatformVersion: "10.0.19041.0",
				targetPlatformMinVersion: "10.0.17763.0");

			// Act
			task.Execute();

			// Assert
			Assert.False(task.Log.HasLoggedErrors);
			Assert.NotNull(task.GeneratedAppxManifest);
			AssertExpectedManifest(task.GeneratedAppxManifest.ItemSpec, taskItem.ItemSpec);
		}

		[Fact]
		public void TaskShouldBeAbleToOverwriteAppxManifest()
		{
			// Arrange
			var taskItem = new TaskItem("testdata/appxmanifest/empty.appxmanifest");
			var task = GetNewTask(
				appxManifests: [taskItem],
				displayVersion: "1.0.0",
				version: "1",
				targetPlatformVersion: "10.0.19041.0",
				targetPlatformMinVersion: "10.0.17763.0");

			// Act
			task.Execute();

			// Assert
			var generatedPath = task.GeneratedAppxManifest.ItemSpec;
			var initial = XDocument.Load(generatedPath).ToString();
			Assert.True(File.Exists(generatedPath));

			// Act
			for (var i = 2; i < 5; i++)
			{
				task.ApplicationVersion = $"{i}";
				task.Execute();

				// Assert
				Assert.Equal(generatedPath, task.GeneratedAppxManifest.ItemSpec);
				Assert.True(File.Exists(task.GeneratedAppxManifest.ItemSpec));
				var updated = XDocument.Load(generatedPath).ToString();

				Assert.NotEqual(initial, updated);
			}
		}

		static void AssertExpectedManifest(string generatedManifestPath, string inputManifestPath)
		{
			var generated = XDocument.Load(generatedManifestPath).ToString();
			var expected = XDocument.Load(inputManifestPath.Replace(".input.", ".expected.")).ToString();

			Assert.Equal(expected, generated);
		}
	}
}
