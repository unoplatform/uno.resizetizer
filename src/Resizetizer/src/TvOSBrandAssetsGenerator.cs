#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using SkiaSharp;

namespace Uno.Resizetizer
{
	/// <summary>
	/// Generates the tvOS "App Icon &amp; Top Shelf Image" brand assets: layered (parallax) app icons and the Top Shelf images.
	/// </summary>
	internal class TvOSBrandAssetsGenerator
	{
		const string FrontLayer = "Front";
		const string BackLayer = "Back";

		// The Top Shelf banner is much taller than the icon, so the foreground only takes this share of its height
		const float TopShelfForegroundRatio = 0.5f;

		static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

		readonly SkiaSharpTools? _backgroundTools;
		readonly SkiaSharpTools? _foregroundTools;

		public TvOSBrandAssetsGenerator(ResizeImageInfo info, string appIconName, string intermediateOutputPath, ILogger logger)
		{
			Info = info;
			AppIconName = appIconName;
			IntermediateOutputPath = intermediateOutputPath;
			Logger = logger;

			var hasBackground = !string.IsNullOrWhiteSpace(info.Filename) && File.Exists(info.Filename);
			var hasForeground = !string.IsNullOrWhiteSpace(info.ForegroundFilename) && File.Exists(info.ForegroundFilename);
			if (!hasBackground && !hasForeground)
			{
				throw new InvalidOperationException("An app icon needs at least one image.");
			}

			if (hasBackground)
			{
				_backgroundTools = SkiaSharpTools.Create(info.IsVector, info.Filename, null, null, null, logger);
			}

			if (hasForeground)
			{
				_foregroundTools = SkiaSharpTools.Create(info.ForegroundIsVector, info.ForegroundFilename, null, null, info.TintColor, logger);
			}
		}

		public ResizeImageInfo Info { get; }

		public string AppIconName { get; }

		public string IntermediateOutputPath { get; }

		public ILogger Logger { get; }

		public string BrandAssetsDirectory =>
			Path.Combine(IntermediateOutputPath, DpiPath.TvOS.BrandAssetsPath.Replace("{name}", AppIconName));

		public IEnumerable<ResizedImageInfo> Generate()
		{
			Logger.Log("tvOS Brand Assets Directory: " + BrandAssetsDirectory);

			var files = new List<string>();

			files.AddRange(GenerateImageStack(DpiPath.TvOS.AppIcon));
			files.AddRange(GenerateImageStack(DpiPath.TvOS.AppStoreIcon));
			files.AddRange(GenerateTopShelf(DpiPath.TvOS.TopShelf));
			files.AddRange(GenerateTopShelf(DpiPath.TvOS.TopShelfWide));

			var contentsFile = Path.Combine(BrandAssetsDirectory, "Contents.json");
			WriteJson(contentsFile, new JsonObject
			{
				["assets"] = new JsonArray(
					BrandAsset(DpiPath.TvOS.AppStoreIcon[0], "primary-app-icon"),
					BrandAsset(DpiPath.TvOS.AppIcon[0], "primary-app-icon"),
					BrandAsset(DpiPath.TvOS.TopShelfWide[0], "top-shelf-image-wide"),
					BrandAsset(DpiPath.TvOS.TopShelf[0], "top-shelf-image")),
				["info"] = XcodeInfo(),
			});
			files.Add(contentsFile);

			return files.Select(f => new ResizedImageInfo { Dpi = new DpiPath("", 1), Filename = f }).ToList();
		}

		IEnumerable<string> GenerateImageStack(DpiPath[] dpis)
		{
			var stackDirectory = Path.Combine(BrandAssetsDirectory, dpis[0].Path);
			var files = new List<string>();

			foreach (var layer in new[] { FrontLayer, BackLayer })
			{
				var layerDirectory = Path.Combine(stackDirectory, layer + ".imagestacklayer");

				files.AddRange(GenerateImageSet(
					Path.Combine(layerDirectory, "Content.imageset"),
					dpis,
					layer,
					(canvas, canvasSize) =>
					{
						if (layer == BackLayer)
						{
							DrawBackground(canvas, canvasSize);
						}
						else
						{
							DrawForeground(canvas, canvasSize, 1f);
						}
					}));

				var layerContentsFile = Path.Combine(layerDirectory, "Contents.json");
				WriteJson(layerContentsFile, new JsonObject { ["info"] = XcodeInfo() });
				files.Add(layerContentsFile);
			}

			var stackContentsFile = Path.Combine(stackDirectory, "Contents.json");
			WriteJson(stackContentsFile, new JsonObject
			{
				["layers"] = new JsonArray(
					new JsonObject { ["filename"] = FrontLayer + ".imagestacklayer" },
					new JsonObject { ["filename"] = BackLayer + ".imagestacklayer" }),
				["info"] = XcodeInfo(),
			});
			files.Add(stackContentsFile);

			return files;
		}

		IEnumerable<string> GenerateTopShelf(DpiPath[] dpis) =>
			GenerateImageSet(
				Path.Combine(BrandAssetsDirectory, dpis[0].Path),
				dpis,
				"TopShelf",
				(canvas, canvasSize) =>
				{
					DrawBackground(canvas, canvasSize);
					DrawForeground(canvas, canvasSize, TopShelfForegroundRatio);
				});

		IEnumerable<string> GenerateImageSet(string imageSetDirectory, DpiPath[] dpis, string imageName, Action<SKCanvas, SKSizeI> draw)
		{
			Directory.CreateDirectory(imageSetDirectory);

			var files = new List<string>();
			var images = new JsonArray();

			foreach (var dpi in dpis)
			{
				var fileName = AppIconName + imageName + dpi.FileSuffix + ".png";
				var destination = Path.Combine(imageSetDirectory, fileName);

				var (canvasSize, _) = SkiaSharpTools.GetCanvasSize(dpi);

				using (var bitmap = new SKBitmap(canvasSize.Width, canvasSize.Height))
				{
					using (var canvas = new SKCanvas(bitmap))
					{
						canvas.Clear(SKColors.Transparent);
						draw(canvas, canvasSize);
					}

					using var stream = File.Create(destination);
					bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
				}

				files.Add(destination);

				foreach (var idiom in dpi.Idioms ?? [])
				{
					images.Add(new JsonObject
					{
						["idiom"] = idiom,
						["filename"] = fileName,
						["scale"] = dpi.Scale.ToString("0", CultureInfo.InvariantCulture) + "x",
					});
				}
			}

			var contentsFile = Path.Combine(imageSetDirectory, "Contents.json");
			WriteJson(contentsFile, new JsonObject
			{
				["images"] = images,
				["info"] = XcodeInfo(),
			});
			files.Add(contentsFile);

			return files;
		}

		// Layers that sit at the back of a tvOS icon must be opaque, so fall back to white when there is no color
		void DrawBackground(SKCanvas canvas, SKSizeI canvasSize)
		{
			canvas.Clear(Info.Color ?? SKColors.White);

			if (_backgroundTools is null)
			{
				return;
			}

			// Fill the whole canvas, cropping the background if its aspect ratio differs
			var originalSize = _backgroundTools.GetOriginalSize();
			var scale = Math.Max(canvasSize.Width / originalSize.Width, canvasSize.Height / originalSize.Height);

			canvas.Save();
			canvas.Translate(
				(canvasSize.Width - originalSize.Width * scale) / 2,
				(canvasSize.Height - originalSize.Height * scale) / 2);
			canvas.Scale(scale, scale);
			_backgroundTools.DrawUnscaled(canvas, scale);
			canvas.Restore();
		}

		// The foreground is fitted into a square as tall as the canvas, just like the square icons of the other platforms
		void DrawForeground(SKCanvas canvas, SKSizeI canvasSize, float heightRatio)
		{
			if (_foregroundTools is null)
			{
				return;
			}

			var originalSize = _foregroundTools.GetOriginalSize();
			var boxSize = canvasSize.Height * heightRatio * (float)Info.ForegroundScale;
			var scale = Math.Min(boxSize / originalSize.Width, boxSize / originalSize.Height);

			canvas.Save();
			canvas.Translate(
				(canvasSize.Width - originalSize.Width * scale) / 2,
				(canvasSize.Height - originalSize.Height * scale) / 2);
			canvas.Scale(scale, scale);
			_foregroundTools.DrawUnscaled(canvas, scale);
			canvas.Restore();
		}

		static JsonObject BrandAsset(DpiPath dpi, string role)
		{
			var size = dpi.Size!.Value;

			return new JsonObject
			{
				["size"] = string.Format(CultureInfo.InvariantCulture, "{0}x{1}", size.Width, size.Height),
				["idiom"] = "tv",
				["filename"] = dpi.Path,
				["role"] = role,
			};
		}

		static JsonObject XcodeInfo() =>
			new()
			{
				["version"] = 1,
				["author"] = "xcode",
			};

		static void WriteJson(string path, JsonObject json)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, json.ToJsonString(JsonOptions));
		}
	}
}
