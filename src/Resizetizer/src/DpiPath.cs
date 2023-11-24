using SkiaSharp;
using System.Diagnostics;
using System.Linq;

namespace Uno.Resizetizer
{
	[DebuggerDisplay("Path: {Path}, Size: {Size}, Scale: {Scale}")]
	internal class DpiPath
	{
		public DpiPath(string path, decimal scale, string nameSuffix = null, string scaleSuffix = null, SKSize? size = null, string[] idioms = null)
		{
			Path = path;
			Scale = scale;
			NameSuffix = nameSuffix;
			ScaleSuffix = scaleSuffix;
			Size = size;
			Idioms = idioms;
		}

		public string Path { get; set; }

		public decimal Scale { get; set; }

		public string FileSuffix =>
			string.Concat(NameSuffix, ScaleSuffix);

		public string NameSuffix { get; set; }

		public string ScaleSuffix { get; set; }

		public SKSize? Size { get; set; }

		public bool Optimize { get; set; } = true;

		public string[] Idioms { get; set; }

		public static class Android
		{
			public static DpiPath[] AppIcon
				=> new[]
				{
					new DpiPath("mipmap-mdpi", 1.0m, size: new SKSize(48, 48)),
					new DpiPath("mipmap-hdpi", 1.5m, size: new SKSize(48, 48)),
					new DpiPath("mipmap-xhdpi", 2.0m, size: new SKSize(48, 48)),
					new DpiPath("mipmap-xxhdpi", 3.0m, size: new SKSize(48, 48)),
					new DpiPath("mipmap-xxxhdpi", 4.0m, size: new SKSize(48, 48)),
				};

			public static DpiPath[] AppIconParts
				=> new[]
				{
					new DpiPath("mipmap-mdpi", 1.0m, size: new SKSize(108, 108)),
					new DpiPath("mipmap-hdpi", 1.5m, size: new SKSize(108, 108)),
					new DpiPath("mipmap-xhdpi", 2.0m, size: new SKSize(108, 108)),
					new DpiPath("mipmap-xxhdpi", 3.0m, size: new SKSize(108, 108)),
					new DpiPath("mipmap-xxxhdpi", 4.0m, size: new SKSize(108, 108)),
				};
		}

		public static class Ios
		{
			public const string AppIconPath = "Assets.xcassets/{name}.appiconset";

			public static DpiPath Original =>
				new DpiPath("Resources", 1.0m);

			public static DpiPath[] AppIcon
				=> new[]
				{
					// Notification
					new DpiPath(AppIconPath, 2.0m, "20x20", "@2x", new SKSize(20, 20), new [] { "iphone", "ipad" }),
					new DpiPath(AppIconPath, 3.0m, "20x20", "@3x", new SKSize(20, 20), new [] { "iphone" }),

					// Settings
					new DpiPath(AppIconPath, 2.0m, "29x29", "@2x", new SKSize(29, 29), new [] { "iphone", "ipad" }),
					new DpiPath(AppIconPath, 3.0m, "29x29", "@3x", new SKSize(29, 29), new [] { "iphone" }),

					// Spotlight
					new DpiPath(AppIconPath, 2.0m, "40x40", "@2x", new SKSize(40, 40), new [] { "iphone", "ipad" }),
					new DpiPath(AppIconPath, 3.0m, "40x40", "@3x", new SKSize(40, 40), new [] { "iphone" }),

					// App Icon - iPhone
					new DpiPath(AppIconPath, 2.0m, "60x60", "@2x", new SKSize(60, 60), new [] { "iphone" }),
					new DpiPath(AppIconPath, 3.0m, "60x60", "@3x", new SKSize(60, 60), new [] { "iphone" }),

					// App Icon - ipad
					new DpiPath(AppIconPath, 2.0m, "76x76", "@2x", new SKSize(76, 76), new [] { "ipad" }),
					new DpiPath(AppIconPath, 2.0m, "83.5x83.5", "@2x", new SKSize(83.5f, 83.5f), new [] { "ipad" }),

					// App Store
					new DpiPath(AppIconPath, 1.0m, "ItunesArtwork", null, new SKSize(1024, 1024), new [] { "ios-marketing" }),
				};
		}

		public static class Windows
		{
			public const string OutputPath = "";
			public const string IconOutputPath = "";

			public static DpiPath Original =>
				new DpiPath(OutputPath, 1.0m, null, ".scale-100");

			public static DpiPath[] Image
				=> new[]
				{
					new DpiPath(OutputPath, 1.00m, null, ""), // Include a 1x version without the scale suffix
					new DpiPath(OutputPath, 1.00m, null, ".scale-100"),
					new DpiPath(OutputPath, 1.25m, null, ".scale-125"),
					new DpiPath(OutputPath, 1.50m, null, ".scale-150"),
					new DpiPath(OutputPath, 2.00m, null, ".scale-200"),
					new DpiPath(OutputPath, 3.00m, null, ".scale-300"),
					new DpiPath(OutputPath, 4.00m, null, ".scale-400"),
				};

			public static DpiPath[] SplashScreen
				=> new[]
				{
					new DpiPath(OutputPath, 1.00m, string.Empty, ".scale-100", new SKSize(620, 300)),
					new DpiPath(OutputPath, 1.25m, string.Empty, ".scale-125", new SKSize(620, 300)),
					new DpiPath(OutputPath, 1.50m, string.Empty, ".scale-150", new SKSize(620, 300)),
					new DpiPath(OutputPath, 2.00m, string.Empty, ".scale-200", new SKSize(620, 300)),
					new DpiPath(OutputPath, 3.00m, string.Empty, ".scale-300", new SKSize(620, 300)),
					new DpiPath(OutputPath, 4.00m, string.Empty, ".scale-400", new SKSize(620, 300)),
				};

			// App Icon
			public static DpiPath[] Logo
				=> new[]
				{
					// normal
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".scale-100", new SKSize(44, 44)),
					new DpiPath(IconOutputPath, 1.25m, "Logo", ".scale-125", new SKSize(44, 44)),
					new DpiPath(IconOutputPath, 1.50m, "Logo", ".scale-150", new SKSize(44, 44)),
					new DpiPath(IconOutputPath, 2.00m, "Logo", ".scale-200", new SKSize(44, 44)),
					new DpiPath(IconOutputPath, 4.00m, "Logo", ".scale-400", new SKSize(44, 44)),
					// targetsize
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".targetsize-16", new SKSize(16, 16)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".targetsize-24", new SKSize(24, 24)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".targetsize-32", new SKSize(32, 32)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".targetsize-48", new SKSize(48, 48)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".targetsize-256", new SKSize(256, 256)),
					// altform-unplated_targetsize
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-unplated_targetsize-16", new SKSize(16, 16)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-unplated_targetsize-24", new SKSize(24, 24)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-unplated_targetsize-32", new SKSize(32, 32)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-unplated_targetsize-48", new SKSize(48, 48)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-unplated_targetsize-256", new SKSize(256, 256)),
					// altform-lightunplated_targetsize
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-lightunplated_targetsize-16", new SKSize(16, 16)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-lightunplated_targetsize-24", new SKSize(24, 24)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-lightunplated_targetsize-32", new SKSize(32, 32)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-lightunplated_targetsize-48", new SKSize(48, 48)),
					new DpiPath(IconOutputPath, 1.00m, "Logo", ".altform-lightunplated_targetsize-256", new SKSize(256, 256)),
				};

			// Store Logo
			public static DpiPath[] StoreLogo
				=> new[]
				{
					new DpiPath(IconOutputPath, 1.00m, "StoreLogo", ".scale-100", new SKSize(50, 50)),
					new DpiPath(IconOutputPath, 1.25m, "StoreLogo", ".scale-125", new SKSize(50, 50)),
					new DpiPath(IconOutputPath, 1.50m, "StoreLogo", ".scale-150", new SKSize(50, 50)),
					new DpiPath(IconOutputPath, 2.00m, "StoreLogo", ".scale-200", new SKSize(50, 50)),
					new DpiPath(IconOutputPath, 4.00m, "StoreLogo", ".scale-400", new SKSize(50, 50)),
				};

			// Small Tile
			public static DpiPath[] SmallTile
				=> new[]
				{
					new DpiPath(IconOutputPath, 1.00m, "SmallTile", ".scale-100", new SKSize(71, 71)),
					new DpiPath(IconOutputPath, 1.25m, "SmallTile", ".scale-125", new SKSize(71, 71)),
					new DpiPath(IconOutputPath, 1.50m, "SmallTile", ".scale-150", new SKSize(71, 71)),
					new DpiPath(IconOutputPath, 2.00m, "SmallTile", ".scale-200", new SKSize(71, 71)),
					new DpiPath(IconOutputPath, 4.00m, "SmallTile", ".scale-400", new SKSize(71, 71)),
				};

			// Medium Tile
			public static DpiPath[] MediumTile
				=> new[]
				{
					new DpiPath(IconOutputPath, 1.00m, "MediumTile", ".scale-100", new SKSize(150, 150)),
					new DpiPath(IconOutputPath, 1.25m, "MediumTile", ".scale-125", new SKSize(150, 150)),
					new DpiPath(IconOutputPath, 1.50m, "MediumTile", ".scale-150", new SKSize(150, 150)),
					new DpiPath(IconOutputPath, 2.00m, "MediumTile", ".scale-200", new SKSize(150, 150)),
					new DpiPath(IconOutputPath, 4.00m, "MediumTile", ".scale-400", new SKSize(150, 150)),
				};

			// Wide Tile
			public static DpiPath[] WideTile
				=> new[]
				{
					new DpiPath(IconOutputPath, 1.00m, "WideTile", ".scale-100", new SKSize(310, 150)),
					new DpiPath(IconOutputPath, 1.25m, "WideTile", ".scale-125", new SKSize(310, 150)),
					new DpiPath(IconOutputPath, 1.50m, "WideTile", ".scale-150", new SKSize(310, 150)),
					new DpiPath(IconOutputPath, 2.00m, "WideTile", ".scale-200", new SKSize(310, 150)),
					new DpiPath(IconOutputPath, 4.00m, "WideTile", ".scale-400", new SKSize(310, 150)),
				};

			// Large Tile
			public static DpiPath[] LargeTile
				=> new[]
				{
					new DpiPath(IconOutputPath, 1.00m, "LargeTile", ".scale-100", new SKSize(310, 310)),
					new DpiPath(IconOutputPath, 1.25m, "LargeTile", ".scale-125", new SKSize(310, 310)),
					new DpiPath(IconOutputPath, 1.50m, "LargeTile", ".scale-150", new SKSize(310, 310)),
					new DpiPath(IconOutputPath, 2.00m, "LargeTile", ".scale-200", new SKSize(310, 310)),
					new DpiPath(IconOutputPath, 4.00m, "LargeTile", ".scale-400", new SKSize(310, 310)),
				};

			// Badge
			public static DpiPath[] Badge
				=> new[]
				{
					new DpiPath(IconOutputPath, 1.00m, "Badge", ".scale-100", new SKSize(24, 24)),
					new DpiPath(IconOutputPath, 1.25m, "Badge", ".scale-125", new SKSize(24, 24)),
					new DpiPath(IconOutputPath, 1.50m, "Badge", ".scale-150", new SKSize(24, 24)),
					new DpiPath(IconOutputPath, 2.00m, "Badge", ".scale-200", new SKSize(24, 24)),
					new DpiPath(IconOutputPath, 4.00m, "Badge", ".scale-400", new SKSize(24, 24)),
				};

			// TODO: logo variants (targetsize, altform-unplated, altform-lightunplated)

			public static DpiPath[] AppIcon =>
				Logo.Union(
				StoreLogo).Union(
				SmallTile).Union(
				MediumTile).Union(
				WideTile).Union(
				LargeTile).ToArray();
		}

		public static class Wasm
		{
			public static DpiPath[] AppIcon => new[]
			{
					new DpiPath("", 1.00m, scaleSuffix:"-16", size: new SKSize(16, 16)),
					new DpiPath("", 1.00m,scaleSuffix:"-32", size: new SKSize(32, 32)),
					new DpiPath("", 1.00m,scaleSuffix:"-128", size: new SKSize(128, 128)),
					new DpiPath("", 1.00m,scaleSuffix:"-512", size: new SKSize(512, 512)),
			};
		}

		public static DpiPath GetOriginal()
		{
			return DpiPath.Windows.Original;
		}

		public static DpiPath[] GetDpis()
		{
			return DpiPath.Windows.Image;
		}

		public static DpiPath[] GetAppIconDpis(string platform, string appIconName)
		{
			DpiPath[] result = null;

			switch (platform.ToLowerInvariant())
			{
				case "ios":
				case "maccatalyst":
					result = DpiPath.Ios.AppIcon;
					break;
				case "android":
					result = DpiPath.Android.AppIcon;
					break;
				case "uwp":
				case "windows":
				case "wpf":
					result = DpiPath.Windows.AppIcon;
					break;
				case "wasm":
					result = DpiPath.Wasm.AppIcon;
					break;
			}

			foreach (var r in result)
			{
				if (!string.IsNullOrEmpty(r.Path))
				{
					r.Path = r.Path.Replace("{name}", appIconName);
				}
			}

			return result;
		}
	}
}
