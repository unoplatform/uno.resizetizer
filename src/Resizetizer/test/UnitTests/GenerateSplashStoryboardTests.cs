using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;

namespace Uno.Resizetizer.Tests
{
	public class GenerateSplashStoryboardTests : MSBuildTaskTestFixture<GenerateSplashStoryboard_v0>
	{
		readonly string _storyboard;

		public GenerateSplashStoryboardTests()
		{
			_storyboard = Path.Combine(DestinationDirectory, "MauiSplash.storyboard");
		}

		protected GenerateSplashStoryboard_v0 GetNewTask(ITaskItem splash) =>
			new()
			{
				OutputFile = _storyboard,
				UnoSplashScreen = new[] { splash },
				BuildEngine = this,
			};

		void AssertFile(string actualPath, string r, string g, string b, string a)
		{
			using var actualStream = File.OpenRead(actualPath);
			var actual = XElement.Load(actualStream);

			using var expectedBuilder = new StringWriter();
			GenerateSplashStoryboard_v0.SubstituteStoryboard(expectedBuilder, GenerateSplashStoryboard_v0.LaunchImage, r, g, b, a);
			var expected = XElement.Parse(expectedBuilder.ToString());

			Assert.True(XNode.DeepEquals(actual, expected), $"{actualPath} did not match:\n{actual}");
		}

		[Theory]
		[InlineData("#abcdef", "0.67058825", "0.8039216", "0.9372549", "1")]
		[InlineData("Green", "0", "0.5019608", "0", "1")]
		public void XmlIsValid(string inputColor, string r, string g, string b, string a)
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
			{
				["BackgroundColor"] = inputColor,
			});

			var task = GetNewTask(splash);
			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			AssertFile(_storyboard, r, g, b, a);
		}

		[Fact]
		public void SdkDefaultWhiteBackgroundIsWritten()
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string> { ["BackgroundColor"] = "#FFFFFF" });

			Assert.True(GetNewTask(splash).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			AssertFile(_storyboard, "appiconfg.png", "1", "1", "1", "1");
		}

		[Fact]
		public void UnsetAndLegacyColorFallBackToWhite()
		{
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string> { ["Color"] = "#FF0000" });

			Assert.True(GetNewTask(splash).Execute(), LogErrorEvents.FirstOrDefault()?.Message);

			AssertFile(_storyboard, "appiconfg.png", "1", "1", "1", "1");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("images/CustomAlias.svg")]
		public void LaunchImageDoesNotDependOnAlias(string alias)
		{
			// The launch images are bundled under a fixed name, whatever the splash file is called
			var splash = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
			{
				["Link"] = alias,
			});

			var task = GetNewTask(splash);
			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			var image = XElement.Load(_storyboard).Descendants("imageView").Single().Attribute("image")!.Value;
			Assert.Equal("uno_splash_launch.png", image);
		}

		[Fact]
		public void ImageIsCenteredWithAutoLayout()
		{
			// A fixed-frame, full-screen image view centers the image on half points (e.g. 393pt wide screens),
			// which resamples it. Auto Layout snaps the frame to device pixels.
			var splash = new TaskItem("images/appiconfg.svg");

			var task = GetNewTask(splash);
			var success = task.Execute();
			Assert.True(success, LogErrorEvents.FirstOrDefault()?.Message);

			var view = XElement.Load(_storyboard).Descendants("view").Single(v => (string)v.Attribute("key") == "view");
			var imageView = view.Descendants("imageView").Single();
			var imageViewId = (string)imageView.Attribute("id");

			Assert.Null(imageView.Attribute("fixedFrame"));
			Assert.Equal("NO", (string)imageView.Attribute("translatesAutoresizingMaskIntoConstraints"));

			var centering = view.Element("constraints").Elements("constraint")
				.Where(c => (string)c.Attribute("firstItem") == imageViewId && (string)c.Attribute("secondItem") == (string)view.Attribute("id"))
				.Select(c => (string)c.Attribute("firstAttribute"))
				.OrderBy(a => a);

			Assert.Equal(new[] { "centerX", "centerY" }, centering);
		}
	}
}
