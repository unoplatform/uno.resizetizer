#nullable enable
//#define WRITE_EXPECTED

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Resizetizer.UnitTests.Mocks;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Uno.Resizetizer.Tests
{
    public class GeneratePackageAppxManifestTests(ITestOutputHelper testOutput) : MSBuildTaskTestFixture<GeneratePackageAppxManifest_v0>(testOutput)
	{
        protected GeneratePackageAppxManifest_v0 GetNewTask(
			string appxManifest,
			string? generatedFilename = null,
			string? applicationId = null,
			string? applicationDisplayVersion = null,
			string? applicationVersion = null,
			string? applicationTitle = null,
			string? description = null,
			string? applicationPublisher = null,
			ITaskItem? appIcon = null,
			ITaskItem? splashScreen = null,
			string? targetPlatformVersion = null,
			string? targetPlatformMinVersion = null,
			string? authors = null) =>
			GetNewTask(
				[new TaskItem(appxManifest)],
				generatedFilename,
				applicationId,
				applicationDisplayVersion,
				applicationVersion,
				applicationTitle,
				description,
				applicationPublisher,
				appIcon,
				splashScreen,
				targetPlatformVersion,
				targetPlatformMinVersion,
				authors);

		protected GeneratePackageAppxManifest_v0 GetNewTask(
			ITaskItem appxManifest,
			string? generatedFilename = null,
			string? applicationId = null,
			string? applicationDisplayVersion = null,
			string? applicationVersion = null,
			string? applicationTitle = null,
			string? description = null,
			string? applicationPublisher = null,
			ITaskItem? appIcon = null,
			ITaskItem? splashScreen = null,
			string? targetPlatformVersion = null,
			string? targetPlatformMinVersion = null,
			string? authors = null) =>
			GetNewTask(
				[appxManifest],
				generatedFilename,
				applicationId,
				applicationDisplayVersion,
				applicationVersion,
				applicationTitle,
				description,
				applicationPublisher,
				appIcon,
				splashScreen,
				targetPlatformVersion,
				targetPlatformMinVersion,
				authors);


		protected GeneratePackageAppxManifest_v0 GetNewTask(
			ITaskItem[] appxManifests,
			string? generatedFilename = null,
			string? applicationId = null,
			string? applicationDisplayVersion = null,
			string? applicationVersion = null,
			string? applicationTitle = null,
			string? description = null,
			string? applicationPublisher = null,
			ITaskItem? appIcon = null,
			ITaskItem? splashScreen = null,
			string? targetPlatformVersion = null,
			string? targetPlatformMinVersion = null,
			string? authors = null)
		{
			return new()
			{
				IntermediateOutputPath = DestinationDirectory,
				BuildEngine = this,
				GeneratedFilename = generatedFilename,
				AppxManifest = appxManifests,
				ApplicationId = applicationId,
				ApplicationDisplayVersion = applicationDisplayVersion,
				ApplicationVersion = applicationVersion,
				ApplicationTitle = applicationTitle,
				Description = description,
				ApplicationPublisher = applicationPublisher,
				AssemblyName = GetType().Assembly.GetName().Name,
				AppIcon = appIcon == null ? null : [appIcon],
				SplashScreen = splashScreen == null ? null : [splashScreen],
				TargetFramework = "windows",
				Authors = authors,
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

			var inputFilename = $"testdata/appxmanifest/manifestTakesPriority.input.appxmanifest";
			var task = GetNewTask(inputFilename,
				applicationId: "com.contoso.myapp",
				applicationDisplayVersion: "2.5",
				applicationVersion: "3",
				applicationTitle: "Fishy Things",
				appIcon: appIcon,
				splashScreen: splashScreen);

			var success = task.Execute();
			Assert.True(success, $"{task.GetType()}.Execute() failed: " + LogErrorEvents.FirstOrDefault()?.Message);

			AssertExpectedManifest(task, inputFilename);
		}

		[Fact]
		public void CorrectGenerationWhenUserSpecifyBackgroundColor()
		{
			var appIcon = MockTaskItem.CreateAppIcon("images/appicon.svg", "images/appiconfg.svg");
			appIcon.SetMetadata("Color", "#FFFFFF");
			var splashScreen = MockTaskItem.CreateSplashScreen("images/dotnet_bot.svg", "#FFFFFF");

			var inputFilename = $"testdata/appxmanifest/correctGenerationWhenUserSpecifyBackgroundColor.input.appxmanifest";
			var task = GetNewTask(inputFilename,
				applicationId: "com.contoso.myapp",
				applicationDisplayVersion: "1.0.0",
				applicationVersion: "1",
				applicationTitle: "Sample App",
				appIcon: appIcon,
				splashScreen: splashScreen);

			var success = task.Execute();

			Assert.True(success, $"{task.GetType()}.Execute() failed: " + LogErrorEvents.FirstOrDefault()?.Message);
			AssertExpectedManifest(task, inputFilename);
		}

		[Fact]
		public void CorrectGenerationWithOutBackgroundColor()
		{
			var appIcon = MockTaskItem.CreateAppIcon("images/appicon.svg", "images/appiconfg.svg");
			var splashScreen = MockTaskItem.CreateSplashScreen("images/dotnet_bot.svg", "#FFFFFF");

			var inputFilename = $"testdata/appxmanifest/correctGenerationWithOutBackgroundColor.input.appxmanifest";
			var task = GetNewTask(inputFilename,
				applicationId: "com.contoso.myapp",
				applicationDisplayVersion: "1.0.0",
				applicationVersion: "1",
				applicationTitle: "Sample App",
				appIcon: appIcon,
				splashScreen: splashScreen);

			var success = task.Execute();
			Assert.True(success, $"{task.GetType()}.Execute() failed: " + LogErrorEvents.FirstOrDefault()?.Message);

			AssertExpectedManifest(task, inputFilename);
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
			var task = GetNewTask(appxManifests: [taskItem, taskItem]);

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
			AssertExpectedManifest(task, taskItem);
		}

		[Theory]
		[InlineData("template")]
		public void GeneratesExpectedAppxManifest(string manifestName)
		{
			// Arrange
			var appIcon = MockTaskItem.CreateAppIcon("images/camera.svg", "images/loginbg.png");
			var splashScreen = MockTaskItem.CreateSplashScreen("images/dotnet_logo.svg", "#FFFFFF");

			var taskItem = $"testdata/appxmanifest/{manifestName}.input.appxmanifest";
			var task = GetNewTask(
				appxManifest: taskItem,
				applicationId: "com.contoso.myapp",
				applicationDisplayVersion: "1.0.0",
				applicationVersion: "1",
				applicationTitle: "Sample App",
				description: "This is a sample from the Unit Tests",
				appIcon: appIcon,
				splashScreen: splashScreen,
				targetPlatformVersion: "10.0.19041.0",
				targetPlatformMinVersion: "10.0.17763.0");

			// Act
			task.Execute();

			// Assert
			AssertExpectedManifest(task, taskItem);
		}

		[Fact]
		public void TaskShouldBeAbleToOverwriteAppxManifest()
		{
			// Arrange
			var taskItem = "testdata/appxmanifest/empty.appxmanifest";
			var task = GetNewTask(
				appxManifest: taskItem,
				applicationDisplayVersion: "1.0.0",
				applicationVersion: "1",
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

		[Fact]
		public void UnoIssue17199()
		{
			var appIcon = MockTaskItem.CreateAppIcon(@"images\appicon.svg", @"images\appiconfg.svg");
			appIcon.SetMetadata("ForegroundScale", "0.65");
			appIcon.SetMetadata("Color", "#00000000");
			appIcon.SetMetadata("IsDefaultItem", "true");

			var splashScreen = MockTaskItem.CreateSplashScreen(@"images\dotnet_bot.svg");
			splashScreen.SetMetadata("BaseSize", "128,128");

			var inputFile = "testdata/appxmanifest/unoIssue17199.input.appxmanifest";

			var task = GetNewTask(
				appxManifest: inputFile,
				applicationTitle: "Sample App",
				description: "SampleApp powered by Uno Platform",
				applicationVersion: "1",
				applicationPublisher: "Uno Platform",
				applicationDisplayVersion: "1.0",
				targetPlatformVersion: "10.0.19041.0",
				targetPlatformMinVersion: "10.0.18362.0",
				applicationId: "com.contoso.sampleapp",
				authors: "Uno Platform",
				appIcon: appIcon,
				splashScreen: splashScreen
			);

			// Act
			task.Execute();

			// Assert
			AssertExpectedManifest(task, inputFile);

		}

		static void AssertExpectedManifest(GeneratePackageAppxManifest_v0 task, string inputManifest) =>
			AssertExpectedManifest(task, new TaskItem(inputManifest));

		static void AssertExpectedManifest(GeneratePackageAppxManifest_v0 task, ITaskItem inputManifest)
		{
			Assert.False(task.Log.HasLoggedErrors);
			Assert.NotNull(task.GeneratedAppxManifest);

			var generated = File.ReadAllText(task.GeneratedAppxManifest.ItemSpec);
			var expectedFile = inputManifest.ItemSpec.Replace(".input.", ".expected.");
#if WRITE_EXPECTED
			File.WriteAllText(expectedFile, generated);
#endif
			var expected = File.ReadAllText(expectedFile);

			Assert.Equal(expected, generated, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
		}
	}
}
