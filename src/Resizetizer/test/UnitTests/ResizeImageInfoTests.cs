#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Build.Utilities;
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

		// https://github.com/unoplatform/uno.resizetizer/issues/420
		public class WinAppSdkAssetScales
		{
			[Fact]
			public void DefaultsMatchVisualStudio()
			{
				var info = ResizeImageInfo.Parse(new TaskItem("images/full_square.svg", new Dictionary<string, string>
				{
					["IsAppIcon"] = bool.TrueString,
				}));

				Assert.Equal(0.75, info.WinAppSdkAppIconScale);
				Assert.Equal(0.5, info.WinAppSdkSmallTileScale);
				Assert.Equal(0.33, info.WinAppSdkTileScale);
				Assert.Equal(1.0, info.WinAppSdkStoreLogoScale);
			}

			[Fact]
			public void MetadataIsParsedWithInvariantCulture()
			{
				var culture = Thread.CurrentThread.CurrentCulture;
				Thread.CurrentThread.CurrentCulture = new CultureInfo("cs-CZ");
				try
				{
					var info = ResizeImageInfo.Parse(new TaskItem("images/full_square.svg", new Dictionary<string, string>
					{
						["IsAppIcon"] = bool.TrueString,
						["WinAppSdkAppIconScale"] = "0.6",
						["WinAppSdkSmallTileScale"] = "0.4",
						["WinAppSdkTileScale"] = "0.25",
						["WinAppSdkStoreLogoScale"] = "0.9",
					}));

					Assert.Equal(0.6, info.WinAppSdkAppIconScale);
					Assert.Equal(0.4, info.WinAppSdkSmallTileScale);
					Assert.Equal(0.25, info.WinAppSdkTileScale);
					Assert.Equal(0.9, info.WinAppSdkStoreLogoScale);
				}
				finally
				{
					Thread.CurrentThread.CurrentCulture = culture;
				}
			}
		}
			// The WinAppSdk* and Desktop* names replace Windows* and Skia*, which keep working as legacy aliases
		[Collection(TargetPlatformCollection.Name)]
		public class PlatformScaleAliases : IDisposable
		{
			readonly string? _targetPlatform = ResizetizeImages_v0.TargetPlatform;

			public void Dispose() => ResizetizeImages_v0.TargetPlatform = _targetPlatform;

			[Theory]
			[InlineData("uwp", "WindowsForegroundScale", "WinAppSdkForegroundScale")]
			[InlineData("wpf", "SkiaForegroundScale", "DesktopForegroundScale")]
			[InlineData("netstandard", "SkiaForegroundScale", "DesktopForegroundScale")]
			public void IconForegroundScale(string platform, string legacy, string name)
			{
				ResizetizeImages_v0.TargetPlatform = platform;

				double Parse(Dictionary<string, string> metadata)
				{
					metadata["IsAppIcon"] = bool.TrueString;
					metadata["ForegroundScale"] = "0.9";
					return ResizeImageInfo.Parse(new TaskItem("images/full_square.svg", metadata)).ForegroundScale;
				}

				AssertAliases(Parse, legacy, name);
			}

			[Theory]
			[InlineData("uwp", "WindowsScale", "WinAppSdkScale")]
			[InlineData("wpf", "SkiaScale", "DesktopScale")]
			[InlineData("netstandard", "SkiaScale", "DesktopScale")]
			public void SplashScreenScale(string platform, string legacy, string name)
			{
				ResizetizeImages_v0.TargetPlatform = platform;

				double Parse(Dictionary<string, string> metadata)
				{
					metadata["IsSplashScreen"] = bool.TrueString;
					metadata["Scale"] = "0.9";
					return ResizeImageInfo.Parse(new TaskItem("images/full_square.svg", metadata)).ForegroundScale;
				}

				AssertAliases(Parse, legacy, name);
			}

			[Theory]
			[InlineData("uwp", "DesktopForegroundScale")]
			[InlineData("uwp", "SkiaForegroundScale")]
			[InlineData("wpf", "WinAppSdkForegroundScale")]
			[InlineData("wpf", "WindowsForegroundScale")]
			public void OtherPlatformNamesAreIgnored(string platform, string name)
			{
				ResizetizeImages_v0.TargetPlatform = platform;

				var info = ResizeImageInfo.Parse(new TaskItem("images/full_square.svg", new Dictionary<string, string>
				{
					["IsAppIcon"] = bool.TrueString,
					["ForegroundScale"] = "0.9",
					[name] = "0.5",
				}));

				Assert.Equal(0.9, info.ForegroundScale, 5);
			}

			static void AssertAliases(Func<Dictionary<string, string>, double> parse, string legacy, string name)
			{
				Assert.Equal(0.9, parse(new()), 5);
				Assert.Equal(0.7, parse(new() { [legacy] = "0.7" }), 5);
				Assert.Equal(0.6, parse(new() { [name] = "0.6" }), 5);
				Assert.Equal(0.6, parse(new() { [legacy] = "0.7", [name] = "0.6" }), 5);
			}
		}
	}
}
