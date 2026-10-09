using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;

namespace Uno.Resizetizer.Tests
{
	public class GenerateSplashAndroidResourcesTests : MSBuildTaskTestFixture<GenerateSplashAndroidResources_v0>
	{
		readonly string _colors;
		readonly string _drawable;
		readonly string _drawable_v31;

		public GenerateSplashAndroidResourcesTests()
		{
			_colors = Path.Combine(DestinationDirectory, "values", "uno_colors.xml");
			_drawable = Path.Combine(DestinationDirectory, "drawable", "uno_splash_image.xml");
			_drawable_v31 = Path.Combine(DestinationDirectory, "drawable-v31", "uno_splash_image.xml");
		}

		protected GenerateSplashAndroidResources_v0 GetNewTask(ITaskItem splash) =>
			new()
			{
				UnoSplashScreen = new[] { splash },
				IntermediateOutputPath = DestinationDirectory,
				BuildEngine = this,
			};

		[Theory]
		[InlineData("#abcdef", "#ffabcdef")]
		[InlineData("Red", "#ffff0000")]
		public void XmlIsValid(string inputColor, string outputColor)
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
			{
				["BackgroundColor"] = inputColor,
			});

			var task = GetNewTask(splash);
			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			AssertColorsFile("uno_colors.xml", outputColor);
			AssertImageFile("uno_splash_image.xml", _drawable, "@drawable/appiconfg_png");
			AssertImageFile("uno_splash_image_v31.xml", _drawable_v31, "@drawable/appiconfg_png");
		}

		[Fact]
		public void AccentColorsDoNotChangeOutput()
		{
			var plain = new TaskItem("images/appiconfg.svg", new Dictionary<string, string> { ["BackgroundColor"] = "#abcdef" });
			Assert.True(GetNewTask(plain).Execute(), LogErrorEvents.FirstOrDefault()?.Message);
			var expectedColors = File.ReadAllText(_colors);
			var expectedDrawable = File.ReadAllText(_drawable);

			var accented = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
			{
				["BackgroundColor"] = "#abcdef",
				["AccentColor"] = "#FF4500",
				["DarkAccentColor"] = "#FFB347",
			});
			Assert.True(GetNewTask(accented).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			Assert.Equal(expectedColors, File.ReadAllText(_colors));
			Assert.Equal(expectedDrawable, File.ReadAllText(_drawable));
		}

		[Theory]
		[InlineData("tall_image.png", "36", "192")]
		[InlineData("wide_image.png", "192", "36")]
		public void XmlIsValidForNonSquare(string image, string width, string height)
		{
			var splash = new TaskItem("images/" + image, new Dictionary<string, string>
			{
				["BackgroundColor"] = "Red",
				["Link"] = "splash_image_drawable",
			});

			var task = GetNewTask(splash);
			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			AssertImageFile("uno_splash_image.xml", _drawable, "@drawable/splash_image_drawable_png", width, height);
			AssertImageFile("uno_splash_image_v31.xml", _drawable_v31, "@drawable/splash_image_drawable_png", width, height);
		}

		[Fact]
		public void SdkDefaultWhiteBackgroundIsWritten()
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string> { ["BackgroundColor"] = "#FFFFFF" });

			Assert.True(GetNewTask(splash).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			AssertColorsFile("uno_colors.xml", "#ffffffff");
		}

		[Fact]
		public void ColorIsIgnored()
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string> { ["Color"] = "#FF0000" });

			Assert.True(GetNewTask(splash).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			Assert.Contains(File.ReadAllLines(_colors), x => x.Contains("#00000000"));
		}

		[Fact]
		public void SplashWithoutColor()
		{
			var splash = new TaskItem("images/" + "tall_image.png", new Dictionary<string, string>
			{
				["Link"] = "splash_image_drawable",
			});

			var task = GetNewTask(splash);

			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			var hasTransparentColor = File.ReadAllLines(_colors).Any(x => x.Contains("#00000000"));

			Assert.True(hasTransparentColor);
		}

		[Theory]
		[InlineData(null, "appiconfg_png")]
		// Assets reaching aapt2 through @(AndroidResource) are lowercased by the Android SDK,
		// so the generated @drawable reference has to be lowercased to match.
		[InlineData("images/CustomAlias.svg", "customalias_png")]
		// Uno retargets the splash image to a drawable named the way its AndroidResourceNameEncoder encodes it
		[InlineData("images/splash-screen.svg", "splash_screen_png")]
		[InlineData("images/1splash.svg", "__1splash_png")]
		public void SplashScreenResectsAlias(string alias, string outputImage)
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
			{
				["Link"] = alias,
			});

			var task = GetNewTask(splash);
			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			AssertImageFile("uno_splash_image.xml", _drawable, $"@drawable/{outputImage}");
			AssertImageFile("uno_splash_image_v31.xml", _drawable_v31, $"@drawable/{outputImage}");
		}

		void AssertColorsFile(string expectedFilename, string color)
		{
			var expectedXml = File.ReadAllText($"testdata/androidsplash/" + expectedFilename)
				.Replace("{uno_splash_color}", color, StringComparison.OrdinalIgnoreCase);

			var actual = XElement.Load(_colors);
			var expected = XElement.Parse(expectedXml);

			Assert.True(XNode.DeepEquals(actual, expected), $"{_colors} did not match:\n{actual}");
		}

		[Fact]
		public void SplashIconIsAnimatableOnAndroid12()
		{
			// Non-Animatable icons are pre-rendered at 108dp and upscaled by the system, making them blurry
			var splash = new TaskItem("images/appiconfg.svg");

			var task = GetNewTask(splash);
			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			var root = XElement.Load(_drawable_v31);
			Assert.Equal("animation-list", root.Name.LocalName);
			Assert.Single(root.Elements("item"));
		}

		void AssertImageFile(string expectedFilename, string actualFilename, string image, string width = "192", string height = "192")
		{
			var expectedXml = File.ReadAllText($"testdata/androidsplash/" + expectedFilename)
				.Replace("{drawable}", image, StringComparison.OrdinalIgnoreCase)
				.Replace("{width}", width, StringComparison.OrdinalIgnoreCase)
				.Replace("{height}", height, StringComparison.OrdinalIgnoreCase);

			var actual = XElement.Load(actualFilename);
			var expected = XElement.Parse(expectedXml);

			Assert.True(XNode.DeepEquals(actual, expected), $"{actualFilename} did not match:\n{actual}");
		}
	}
}
