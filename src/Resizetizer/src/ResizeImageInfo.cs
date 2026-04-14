#nullable enable
using Microsoft.Build.Framework;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Uno.Resizetizer
{
	[DebuggerDisplay("OutputName: {OutputName}, OutputPath: {OutputPath}, IsAppIcon: {IsAppIcon}")]
	internal class ResizeImageInfo
	{
		public string? Alias { get; set; }

		public string? Filename { get; set; }

		public string OutputPath =>
			string.IsNullOrWhiteSpace(Alias)
				? string.IsNullOrWhiteSpace(Filename)
					? Path.GetDirectoryName(ForegroundFilename)
					: Path.GetDirectoryName(Filename)
				: Path.GetDirectoryName(Alias);


		public string OutputName =>
			string.IsNullOrWhiteSpace(Alias)
				? string.IsNullOrWhiteSpace(Filename)
					? Path.GetFileNameWithoutExtension(ForegroundFilename)
					: Path.GetFileNameWithoutExtension(Filename)
				: Path.GetFileNameWithoutExtension(Alias);

		public string OutputExtension =>
			string.IsNullOrWhiteSpace(Alias) || !Path.HasExtension(Alias)
				? string.IsNullOrWhiteSpace(Filename) || !Path.HasExtension(Filename)
					? Path.GetExtension(ForegroundFilename)
					: Path.GetExtension(Filename)
				: Path.GetExtension(Alias);

		public SKSize? BaseSize { get; set; }

		public bool Resize { get; set; } = true;

		public SKColor? TintColor { get; set; }

		public SKColor? Color { get; set; }

		public SKColor? DarkColor { get; set; }

		public string? DarkFilename { get; set; }

		public bool IsVector => IsVectorFilename(Filename);

		public bool DarkIsVector => IsVectorFilename(DarkFilename);

		public bool HasDarkOverride => DarkFilename != null
			|| (DarkColor.HasValue && Color.HasValue && DarkColor.Value != Color.Value);

		public bool IsAppIcon { get; set; }

		public bool IsSplashScreen { get; set; }

		public string? ForegroundFilename { get; set; }

		public bool ForegroundIsVector => IsVectorFilename(ForegroundFilename);

		public double ForegroundScale { get; set; } = 1.0;

		private static bool IsVectorFilename(string? filename)
			=> Path.GetExtension(filename)?.Equals(".svg", StringComparison.OrdinalIgnoreCase) ?? false;

		public static ResizeImageInfo Parse(ITaskItem image)
			=> Parse(new[] { image })[0];

		/// <summary>
		/// Applies the splash-screen-specific light/dark background defaults (#F3F3F3 / #202020)
		/// when no color has been declared (FR-011), and resolves DarkColor's per-attribute
		/// fallback to the declared light color when only light was declared (FR-005).
		/// Must be called by splash-screen generators after Parse, since Parse itself does not
		/// receive the IsSplashScreen signal when the item is fed in directly from a splash task.
		/// </summary>
		public void ApplySplashScreenDefaults()
		{
			var lightWasDefaulted = false;
			if (!Color.HasValue)
			{
				Color = new SKColor(0xF3, 0xF3, 0xF3);
				lightWasDefaulted = true;
			}
			if (!DarkColor.HasValue)
			{
				DarkColor = lightWasDefaulted
					? new SKColor(0x20, 0x20, 0x20)
					: Color;
			}
		}

		public static List<ResizeImageInfo> Parse(IEnumerable<ITaskItem>? images)
		{
			var r = new List<ResizeImageInfo>();

			if (images is null)
			{
				return r;
			}

			foreach (var image in images)
			{
				var info = new ResizeImageInfo();

				var fileInfo = new FileInfo(image.GetMetadata("FullPath"));
				if (!fileInfo.Exists)
				{
					throw new FileNotFoundException("Unable to find background file: " + fileInfo.FullName, fileInfo.FullName);
				}

				info.Filename = fileInfo.FullName;

				info.Alias = image.GetMetadata("Link");

				if (string.IsNullOrWhiteSpace(info.Alias))
				{
					var projDirectory = image.GetMetadata("DefiningProjectDirectory");

					if (!string.IsNullOrWhiteSpace(projDirectory))
					{
						info.Alias = fileInfo.FullName.Replace(projDirectory, string.Empty);
					}
				}

				info.BaseSize = Utils.ParseSizeString(image.GetMetadata(nameof(BaseSize)));

				if (bool.TryParse(image.GetMetadata(nameof(Resize)), out var rz))
				{
					info.Resize = rz;
				}
				else if (info.BaseSize == null && !info.IsVector)
				{
					// By default do not resize non-vector images
					info.Resize = false;
				}

				var tintColor = image.GetMetadata(nameof(TintColor));
				info.TintColor = Utils.ParseColorString(tintColor);
				if (info.TintColor is null && !string.IsNullOrEmpty(tintColor))
				{
					throw new InvalidDataException($"Unable to parse color value '{tintColor}' for '{info.Filename}'.");
				}

				var color = image.GetMetadata(nameof(Color));
				var backgroundColor = image.GetMetadata("BackgroundColor");
				if (!string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(backgroundColor))
				{
					throw new InvalidDataException(
						$"UnoSplashScreen item '{image.ItemSpec}' declares both Color and BackgroundColor; use only one (BackgroundColor is preferred).");
				}
				var effectiveColor = !string.IsNullOrEmpty(backgroundColor) ? backgroundColor : color;
				var colorMetadataName = !string.IsNullOrEmpty(backgroundColor) ? "BackgroundColor" : nameof(Color);
				info.Color = Utils.ParseColorString(effectiveColor);
				if (info.Color is null && !string.IsNullOrEmpty(effectiveColor))
				{
					throw new InvalidDataException(
						$"UnoSplashScreen item '{image.ItemSpec}' has an invalid {colorMetadataName}='{effectiveColor}'.");
				}

				var darkBackgroundColor = image.GetMetadata("DarkBackgroundColor");
				info.DarkColor = Utils.ParseColorString(darkBackgroundColor);
				if (info.DarkColor is null && !string.IsNullOrEmpty(darkBackgroundColor))
				{
					throw new InvalidDataException(
						$"UnoSplashScreen item '{image.ItemSpec}' has an invalid DarkBackgroundColor='{darkBackgroundColor}'.");
				}

				var darkImage = image.GetMetadata("DarkImage");
				if (!string.IsNullOrEmpty(darkImage))
				{
					var darkFileInfo = new FileInfo(darkImage);
					if (!darkFileInfo.Exists)
					{
						// Try resolving relative to the defining project directory
						var projectDir = image.GetMetadata("DefiningProjectDirectory");
						if (!string.IsNullOrEmpty(projectDir))
						{
							darkFileInfo = new FileInfo(Path.Combine(projectDir, darkImage));
						}
					}
					if (!darkFileInfo.Exists)
					{
						throw new FileNotFoundException(
							$"UnoSplashScreen item '{image.ItemSpec}' declares DarkImage='{darkImage}' but the file does not exist.",
							darkFileInfo.FullName);
					}
					info.DarkFilename = darkFileInfo.FullName;
				}

				if (bool.TryParse(image.GetMetadata(nameof(IsAppIcon)), out var iai))
				{
					info.IsAppIcon = iai;
				}

				if (bool.TryParse(image.GetMetadata(nameof(IsSplashScreen)), out var isc))
				{
					info.IsSplashScreen = isc;
				}

				if (float.TryParse(image.GetMetadata(nameof(ForegroundScale)), NumberStyles.Number, CultureInfo.InvariantCulture, out var fsc))
				{
					info.ForegroundScale = fsc;
				}

				if (info.IsSplashScreen)
				{
					SetPlatformForegroundScale(image, "Scale", info);
					ApplyPlatformScale(image, info);
				}

				var fgFile = image.GetMetadata("ForegroundFile");
				if (!string.IsNullOrEmpty(fgFile))
				{
					var fgFileInfo = new FileInfo(fgFile);
					if (!fgFileInfo.Exists)
					{
						throw new FileNotFoundException("Unable to find foreground file: " + fgFileInfo.FullName, fgFileInfo.FullName);
					}

					info.ForegroundFilename = fgFileInfo.FullName;
				}

				if (info.IsAppIcon)
				{
					// make sure the image is a foreground if this is an icon
					if (string.IsNullOrEmpty(info.ForegroundFilename))
					{
						info.ForegroundFilename = info.Filename;
						info.Filename = null;
					}

					ApplyPlatformForegroundScale(image, info);
				}


				// TODO:
				// - Parse out custom DPI's

				r.Add(info);
			}

			return r;
		}

		static void SetPlatformForegroundScale(ITaskItem image, string property, ResizeImageInfo info)
		{
			if (double.TryParse(image.GetMetadata(property), NumberStyles.Number,
					CultureInfo.InvariantCulture, out var result))
			{
				info.ForegroundScale = result;
			}
		}

		static void ApplyPlatformForegroundScale(ITaskItem image, ResizeImageInfo info)
		{
			switch (ResizetizeImages_v0.TargetPlatform)
			{
				case "android":
					SetPlatformForegroundScale(image, "AndroidForegroundScale", info);
					break;
				case "ios":
					SetPlatformForegroundScale(image, "IOSForegroundScale", info);
					break;
				case "uwp":
					SetPlatformForegroundScale(image, "WindowsForegroundScale", info);
					break;
				case "wasm":
					SetPlatformForegroundScale(image, "WasmForegroundScale", info);
					break;
				//skia
				case "netstandard" or "wpf":
					SetPlatformForegroundScale(image, "SkiaForegroundScale", info);
					break;
			}
		}


		static void ApplyPlatformScale(ITaskItem image, ResizeImageInfo info)
		{
			switch (ResizetizeImages_v0.TargetPlatform)
			{
				case "android":
					SetPlatformForegroundScale(image, "AndroidScale", info);
					break;
				case "ios":
					SetPlatformForegroundScale(image, "IOSScale", info);
					break;
				case "uwp":
					SetPlatformForegroundScale(image, "WindowsScale", info);
					break;
				case "wasm":
					SetPlatformForegroundScale(image, "WasmScale", info);
					break;
				//skia
				case "netstandard" or "wpf":
					SetPlatformForegroundScale(image, "SkiaScale", info);
					break;
			}
		}
	}
}
