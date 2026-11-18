#nullable enable
using System;
using System.IO;
using System.Linq;
using SkiaSharp;
using Xunit;

namespace Uno.Resizetizer.Tests
{
	// https://github.com/unoplatform/uno.resizetizer/issues/420
	public class WindowsAssetPaddingTests : BaseTest
	{
		readonly TestLogger Logger = new();

		// Art box / canvas shorter side, as Visual Studio's asset generator pads them
		public static TheoryData<string, double> DefaultRatios => new()
		{
			{ "Logo.scale-100", 0.75 },
			{ "Logo.scale-200", 0.75 },
			{ "Logo.scale-400", 0.75 },
			{ "Logo.targetsize-48", 0.75 },
			{ "Logo.targetsize-256", 0.75 },
			{ "Logo.altform-unplated_targetsize-48", 1.0 },
			{ "Logo.altform-unplated_targetsize-256", 1.0 },
			{ "Logo.altform-lightunplated_targetsize-48", 1.0 },
			{ "Logo.altform-lightunplated_targetsize-256", 1.0 },
			{ "StoreLogo.scale-100", 1.0 },
			{ "StoreLogo.scale-400", 1.0 },
			{ "SmallTile.scale-100", 0.5 },
			{ "SmallTile.scale-400", 0.5 },
			{ "MediumTile.scale-100", 0.33 },
			{ "MediumTile.scale-400", 0.33 },
			{ "WideTile.scale-100", 0.33 },
			{ "WideTile.scale-400", 0.33 },
			{ "LargeTile.scale-100", 0.33 },
			{ "LargeTile.scale-400", 0.33 },
		};

		[Theory]
		[MemberData(nameof(DefaultRatios))]
		public void DefaultRatioMatchesVisualStudio(string asset, double expected)
		{
			var info = CreateInfo();

			AssertArtRatio(Render(info, GetDpi(asset)), expected);
		}

		[Theory]
		[InlineData("Logo.scale-200", 0.75 * 0.8)]
		[InlineData("Logo.targetsize-256", 0.75 * 0.8)]
		[InlineData("Logo.altform-unplated_targetsize-256", 0.8)]
		[InlineData("StoreLogo.scale-200", 0.8)]
		[InlineData("SmallTile.scale-200", 0.5 * 0.8)]
		[InlineData("MediumTile.scale-200", 0.33 * 0.8)]
		[InlineData("WideTile.scale-200", 0.33 * 0.8)]
		[InlineData("LargeTile.scale-200", 0.33 * 0.8)]
		public void RatioIsMultipliedByForegroundScale(string asset, double expected)
		{
			var info = CreateInfo();
			info.ForegroundScale = 0.8;

			AssertArtRatio(Render(info, GetDpi(asset)), expected);
		}

		[Theory]
		[InlineData(nameof(ResizeImageInfo.WindowsAppIconScale), "Logo.scale-200", 0.6)]
		[InlineData(nameof(ResizeImageInfo.WindowsAppIconScale), "Logo.targetsize-256", 0.6)]
		[InlineData(nameof(ResizeImageInfo.WindowsSmallTileScale), "SmallTile.scale-200", 0.4)]
		[InlineData(nameof(ResizeImageInfo.WindowsTileScale), "MediumTile.scale-200", 0.5)]
		[InlineData(nameof(ResizeImageInfo.WindowsTileScale), "WideTile.scale-200", 0.5)]
		[InlineData(nameof(ResizeImageInfo.WindowsTileScale), "LargeTile.scale-200", 0.5)]
		[InlineData(nameof(ResizeImageInfo.WindowsStoreLogoScale), "StoreLogo.scale-200", 0.8)]
		public void RatioCanBeOverridden(string property, string asset, double value)
		{
			var info = CreateInfo();
			typeof(ResizeImageInfo).GetProperty(property)!.SetValue(info, value);

			AssertArtRatio(Render(info, GetDpi(asset)), value);
		}

		[Theory]
		[InlineData("Logo.scale-200", 1.0)]
		[InlineData("Logo.altform-unplated_targetsize-256", 1.0)]
		[InlineData("StoreLogo.scale-200", 1.0)]
		[InlineData("SmallTile.scale-200", 0.5)]
		[InlineData("MediumTile.scale-200", 0.33)]
		public void OverrideOnlyAffectsItsOwnAssets(string asset, double expected)
		{
			var info = CreateInfo();
			info.WindowsAppIconScale = 1.0;

			AssertArtRatio(Render(info, GetDpi(asset)), expected);
		}

		[Theory]
		[InlineData(null, "full_square.svg")]
		[InlineData("appicon.svg", "appiconfg.svg")]
		[InlineData("dotnet_background.svg", "dotnet_logo.svg")]
		public void AllOverridesAtOneReproducePreviousOutput(string? background, string foreground)
		{
			var info = CreateInfo(background, foreground);
			info.WindowsAppIconScale = 1.0;
			info.WindowsSmallTileScale = 1.0;
			info.WindowsTileScale = 1.0;
			info.WindowsStoreLogoScale = 1.0;

			foreach (var dpi in DpiPath.Windows.AppIcon)
			{
				Assert.True(
					Render(info, dpi).SequenceEqual(Render(info, WithoutAssetType(dpi))),
					$"{dpi.FileSuffix} differs from the unpadded output");
			}
		}

		[Theory]
		[InlineData(null, "full_square.svg")]
		[InlineData("appicon.svg", "appiconfg.svg")]
		public void UnplatedIconsAndStoreLogoAreUnchangedByDefault(string? background, string foreground)
		{
			var info = CreateInfo(background, foreground);

			var unpadded = DpiPath.Windows.AppIcon
				.Where(d => d.FileSuffix.Contains("unplated") || d.NameSuffix == "StoreLogo");

			foreach (var dpi in unpadded)
			{
				Assert.True(
					Render(info, dpi).SequenceEqual(Render(info, WithoutAssetType(dpi))),
					$"{dpi.FileSuffix} differs from the unpadded output");
			}
		}

		[Fact]
		public void BackgroundColorStillFillsTheTile()
		{
			var info = CreateInfo();
			info.Color = SKColors.Blue;

			using var bitmap = SKBitmap.Decode(Render(info, GetDpi("MediumTile.scale-100")));

			Assert.Equal(SKColors.Blue, bitmap.GetPixel(0, 0));
			Assert.Equal(SKColors.Blue, bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1));
			Assert.Equal(SKColors.Red, bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2));
		}

		[Fact]
		public void BackgroundFileStillFillsTheTile()
		{
			var info = CreateInfo("full_square.svg", "full_square.svg");
			info.WindowsTileScale = 0.0001;

			using var bitmap = SKBitmap.Decode(Render(info, GetDpi("MediumTile.scale-100")));

			Assert.Equal(SKColors.Red, bitmap.GetPixel(0, 0));
			Assert.Equal(SKColors.Red, bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1));
		}

		[Fact]
		public void SplashScreenAndIcoAreNotPadded()
		{
			var info = CreateInfo();
			info.ForegroundScale = 0.9;

			Assert.Equal(0.9, info.GetForegroundScale(DpiPath.Windows.SplashScreen[0]));
			Assert.Equal(0.9, info.GetForegroundScale(new DpiPath("icon", 1.0m, size: new SKSize(64, 64))));
		}

		static DpiPath GetDpi(string asset) =>
			DpiPath.Windows.AppIcon.Single(d => d.FileSuffix == asset);

		static DpiPath WithoutAssetType(DpiPath dpi) =>
			new(dpi.Path, dpi.Scale, dpi.NameSuffix, dpi.ScaleSuffix, dpi.Size);

		static ResizeImageInfo CreateInfo(string? background = null, string foreground = "full_square.svg") =>
			new()
			{
				Filename = background is null ? null : "images/" + background,
				ForegroundFilename = "images/" + foreground,
				IsAppIcon = true,
			};

		byte[] Render(ResizeImageInfo info, DpiPath dpi)
		{
			var destination = Path.Combine(DestinationDirectory, Path.GetRandomFileName() + ".png");
			new SkiaSharpAppIconTools(info, Logger).Resize(dpi, destination);
			return File.ReadAllBytes(destination);
		}

		static void AssertArtRatio(byte[] png, double expected)
		{
			using var bitmap = SKBitmap.Decode(png);

			int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
			for (var y = 0; y < bitmap.Height; y++)
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					if (bitmap.GetPixel(x, y).Alpha > 127)
					{
						minX = Math.Min(minX, x);
						minY = Math.Min(minY, y);
						maxX = Math.Max(maxX, x);
						maxY = Math.Max(maxY, y);
					}
				}
			}

			Assert.True(maxX >= 0, "The image has no visible art");

			var shortSide = Math.Min(bitmap.Width, bitmap.Height);
			var artWidth = maxX - minX + 1;
			var artHeight = maxY - minY + 1;
			var tolerance = 2.0 / shortSide;

			Assert.InRange(artWidth / (double)shortSide, expected - tolerance, expected + tolerance);
			Assert.InRange(artHeight / (double)shortSide, expected - tolerance, expected + tolerance);

			// centered on the canvas
			Assert.InRange((minX + maxX + 1) / 2.0, bitmap.Width / 2.0 - 1.5, bitmap.Width / 2.0 + 1.5);
			Assert.InRange((minY + maxY + 1) / 2.0, bitmap.Height / 2.0 - 1.5, bitmap.Height / 2.0 + 1.5);
		}
	}
}
