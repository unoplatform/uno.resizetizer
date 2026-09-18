using System;
using System.IO;
using System.Text.RegularExpressions;
using SkiaSharp;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Uno.Resizetizer.Tests")]

namespace Uno.Resizetizer
{
	internal class Utils
	{
		static readonly Regex rxResourceFilenameValidation
			= new Regex(@"^[a-z]+[a-z0-9_]{0,}[^_]$", RegexOptions.Singleline | RegexOptions.Compiled);

		public static bool IsValidResourceFilename(string filename)
			=> rxResourceFilenameValidation.IsMatch(Path.GetFileNameWithoutExtension(filename));

		public static SKColor? ParseColorString(string tint)
		{
			if (string.IsNullOrEmpty(tint))
			{
				return null;
			}

			if (SKColor.TryParse(tint, out var color))
			{
				return color;
			}

			if (ColorTable.TryGetNamedColor(tint, out color))
			{
				return color;
			}

			return null;
		}

		public static SKSize? ParseSizeString(string size)
		{
			if (string.IsNullOrEmpty(size))
			{
				return null;
			}

			var parts = size.Split(new char[] { ',', ';' }, 2);

			if (parts.Length > 0 && int.TryParse(parts[0], out var width))
			{
				if (parts.Length > 1 && int.TryParse(parts[1], out var height))
				{
					return new SKSize(width, height);
				}
				else
				{
					return new SKSize(width, width);
				}
			}

			return null;
		}

		public static ResizedImageInfo GenerateIcoFile(string intermediateOutputPath, ILogger logger, ResizeImageInfo info, string iconName = null)
		{
			string destinationFolder = intermediateOutputPath;
			
			string fileName = iconName is null ? Path.GetFileNameWithoutExtension(info.OutputName) : iconName;
			string destination = Path.Combine(destinationFolder, $"{fileName}.ico");
			Directory.CreateDirectory(destinationFolder);

			var (sourceExists, sourceModified) = FileExists(info.Filename);
			var (destinationExists, destinationModified) = FileExists(destination);

			logger.Log($"Generating ICO: {destination}");

			var tools = new SkiaSharpAppIconTools(info, logger);
			var dpi = new DpiPath(fileName, 1.0m, size: new SKSize(256, 256));
			var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
			var entries = new (int Size, byte[] Data)[sizes.Length];
			for (var i = 0; i < sizes.Length; i++)
			{
				var size = sizes[i];
				dpi = new DpiPath(fileName, 1.0m, size: new SKSize(size, size));
				using var memoryStream = new MemoryStream();
				tools.Resize(dpi, destination, () => memoryStream);
				entries[i] = (size, memoryStream.ToArray());
			}

			if (destinationModified > sourceModified)
			{
				logger.Log($"Skipping `{info.Filename}` => `{destination}` file is up to date.");
				return new ResizedImageInfo { Dpi = dpi, Filename = destination };
			}

			using BinaryWriter writer = new BinaryWriter(File.Create(destination));
			writer.Write((short)0x0); // Reserved. Must always be 0.
			writer.Write((short)0x1); // Specifies image type: 1 for icon (.ICO) image
			writer.Write((short)entries.Length); // Specifies number of images in the file.

			var offset = 6 + (16 * entries.Length);
			foreach (var (size, data) in entries)
			{
				var normalizedSize = size == 256 ? 0 : size;
				writer.Write((byte)normalizedSize); // Width in pixels, 0 means 256
				writer.Write((byte)normalizedSize); // Height in pixels, 0 means 256
				writer.Write((byte)0x0); // Specifies number of colors in the color palette
				writer.Write((byte)0x0); // Reserved. Should be 0
				writer.Write((short)0x1); // Specifies color planes. Should be 0 or 1
				writer.Write((short)0x20); // Specifies bits per pixel, 32 for PNG data
				writer.Write(data.Length); // Specifies the size of the image's data in bytes
				writer.Write(offset); // Specifies the offset of PNG data from the beginning of the ICO/CUR file
				offset += data.Length;
			}

			foreach (var (_, data) in entries)
			{
				writer.Write(data);
			}

			writer.Flush();

			return new ResizedImageInfo { Dpi = new DpiPath(fileName, 1.0m, size: new SKSize(256, 256)), Filename = destination };
		}

		public static string SkiaColorWithoutAlpha(SKColor? skColor)
		{
			var result = skColor?.ToString() ?? "transparent";
			if (!result.StartsWith("#"))
			{
				return result;
			}

			// Getting everything after '#ff'
			result = result.Substring(3);
			return "#" + result;
		}

		public static (bool Exists, DateTime Modified) FileExists(string path)
		{
			var exists = File.Exists(path);
			var modified = exists ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
			return (exists, modified);
		}
	}
}
