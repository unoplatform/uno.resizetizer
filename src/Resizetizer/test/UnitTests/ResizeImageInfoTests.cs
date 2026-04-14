using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Utilities;
using SkiaSharp;
using Xunit;

namespace Uno.Resizetizer.Tests
{
	public class ResizeImageInfoTests
	{
		public class IsVector
		{
			[Theory]
			[InlineData("image.svg", true)]
			[InlineData("IMAGE.svg", true)]
			[InlineData("image.SVG", true)]
			[InlineData("IMAGE.SVG", true)]
			[InlineData("image.jpeg", false)]
			[InlineData("IMAGE.jpeg", false)]
			[InlineData("image.JPEG", false)]
			[InlineData("IMAGE.JPEG", false)]
			[InlineData("image.png", false)]
			[InlineData("IMAGE.png", false)]
			[InlineData("image.PNG", false)]
			[InlineData("IMAGE.PNG", false)]
			public void ReturnsCorrectFolder(string filename, bool isVector)
			{
				var info = new ResizeImageInfo
				{
					Filename = filename
				};

				Assert.Equal(isVector, info.IsVector);
			}

			[Theory]
			[InlineData("image")]
			[InlineData("IMAGE")]
			public void SupportsNoExtension(string filename)
			{
				var info = new ResizeImageInfo
				{
					Filename = filename
				};

				Assert.False(info.IsVector);
			}

			[Theory]
			[InlineData("")]
			[InlineData(null)]
			public void DoesNotCrashOnNullOrEmpty(string filename)
			{
				var info = new ResizeImageInfo
				{
					Filename = filename
				};

				Assert.False(info.IsVector);
			}
		}

		public class DarkSplashParsing
		{
			[Fact]
			public void BackgroundColorMetadataParses()
			{
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["BackgroundColor"] = "#FFFFFF",
				});

				var info = ResizeImageInfo.Parse(item);

				Assert.Equal(new SKColor(0xFF, 0xFF, 0xFF), info.Color);
			}

			[Fact]
			public void LegacyColorAliasStillParses()
			{
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["Color"] = "#512BD4",
				});

				var info = ResizeImageInfo.Parse(item);

				Assert.Equal(new SKColor(0x51, 0x2B, 0xD4), info.Color);
			}

			[Fact]
			public void DeclaringBothColorAndBackgroundColorErrors()
			{
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["Color"] = "#FFFFFF",
					["BackgroundColor"] = "#000000",
				});

				var ex = Assert.Throws<InvalidDataException>(() => ResizeImageInfo.Parse(item));
				Assert.Contains("both Color and BackgroundColor", ex.Message);
			}

			[Fact]
			public void DarkBackgroundColorParses()
			{
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["BackgroundColor"] = "#FFFFFF",
					["DarkBackgroundColor"] = "#000000",
				});

				var info = ResizeImageInfo.Parse(item);

				Assert.Equal(new SKColor(0xFF, 0xFF, 0xFF), info.Color);
				Assert.Equal(new SKColor(0x00, 0x00, 0x00), info.DarkColor);
			}

			[Fact]
			public void InvalidDarkBackgroundColorErrors()
			{
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["DarkBackgroundColor"] = "not-a-color",
				});

				var ex = Assert.Throws<InvalidDataException>(() => ResizeImageInfo.Parse(item));
				Assert.Contains("DarkBackgroundColor", ex.Message);
			}

			[Fact]
			public void DarkImageValidPathIsResolved()
			{
				var darkPath = Path.GetFullPath("images/appicon.svg");
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["DarkImage"] = darkPath,
				});

				var info = ResizeImageInfo.Parse(item);

				Assert.NotNull(info.DarkFilename);
				Assert.True(File.Exists(info.DarkFilename));
			}

			[Fact]
			public void MissingDarkImageErrors()
			{
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["DarkImage"] = "images/does_not_exist.svg",
				});

				var ex = Assert.Throws<FileNotFoundException>(() => ResizeImageInfo.Parse(item));
				Assert.Contains("DarkImage", ex.Message);
				Assert.Contains("does not exist", ex.Message);
			}

			[Fact]
			public void ApplySplashScreenDefaultsInjectsBothWhenNeitherDeclared()
			{
				var info = new ResizeImageInfo();

				info.ApplySplashScreenDefaults();

				Assert.Equal(new SKColor(0xF3, 0xF3, 0xF3), info.Color);
				Assert.Equal(new SKColor(0x20, 0x20, 0x20), info.DarkColor);
			}

			[Fact]
			public void ApplySplashScreenDefaultsFallsBackDarkToLightWhenLightDeclared()
			{
				var info = new ResizeImageInfo { Color = new SKColor(0x51, 0x2B, 0xD4) };

				info.ApplySplashScreenDefaults();

				Assert.Equal(new SKColor(0x51, 0x2B, 0xD4), info.Color);
				Assert.Equal(new SKColor(0x51, 0x2B, 0xD4), info.DarkColor);
			}

			[Fact]
			public void ApplySplashScreenDefaultsPreservesDarkColorWhenDeclared()
			{
				var info = new ResizeImageInfo
				{
					Color = new SKColor(0xFF, 0xFF, 0xFF),
					DarkColor = new SKColor(0x00, 0x00, 0x00),
				};

				info.ApplySplashScreenDefaults();

				Assert.Equal(new SKColor(0xFF, 0xFF, 0xFF), info.Color);
				Assert.Equal(new SKColor(0x00, 0x00, 0x00), info.DarkColor);
			}

			[Fact]
			public void HasDarkOverrideIsFalseWithoutDarkMetadata()
			{
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["BackgroundColor"] = "#512BD4",
				});

				var info = ResizeImageInfo.Parse(item);
				info.ApplySplashScreenDefaults();

				Assert.False(info.HasDarkOverride);
			}

			[Fact]
			public void HasDarkOverrideIsTrueWithDarkColor()
			{
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["BackgroundColor"] = "#FFFFFF",
					["DarkBackgroundColor"] = "#000000",
				});

				var info = ResizeImageInfo.Parse(item);
				info.ApplySplashScreenDefaults();

				Assert.True(info.HasDarkOverride);
			}

			[Fact]
			public void HasDarkOverrideIsTrueWithDarkImage()
			{
				var darkPath = Path.GetFullPath("images/appicon.svg");
				var item = new TaskItem("images/appiconfg.svg", new Dictionary<string, string>
				{
					["BackgroundColor"] = "#FFFFFF",
					["DarkImage"] = darkPath,
				});

				var info = ResizeImageInfo.Parse(item);
				info.ApplySplashScreenDefaults();

				Assert.True(info.HasDarkOverride);
			}
		}
	}
}
