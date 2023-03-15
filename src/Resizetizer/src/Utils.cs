using System.IO;
using System.Text.RegularExpressions;
using SkiaSharp;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Microsoft.Maui.Resizetizer.Tests")]

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
				return null;

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
				return null;

			var parts = size.Split(new char[] { ',', ';' }, 2);

			if (parts.Length > 0 && int.TryParse(parts[0], out var width))
			{
				if (parts.Length > 1 && int.TryParse(parts[1], out var height))
					return new SKSize(width, height);
				else
					return new SKSize(width, width);
			}

			return null;
		}

		public static ResizedImageInfo GenerateIcoFile(string intermediateOutputPath, ILogger logger, ResizeImageInfo info, string iconName = null)
		{
			string destinationFolder = intermediateOutputPath;
			
			string fileName = iconName is null ? Path.GetFileNameWithoutExtension(info.OutputName) : iconName;
			string destination = Path.Combine(destinationFolder, $"{fileName}.ico");
			Directory.CreateDirectory(destinationFolder);

			logger.Log($"Generating ICO: {destination}");

			var tools = new SkiaSharpAppIconTools(info, logger);
			var dpi = new DpiPath(fileName, 1.0m, size: new SKSize(64, 64));

			MemoryStream memoryStream = new MemoryStream();
			tools.Resize(dpi, destination, () => memoryStream);
			memoryStream.Position = 0;

			int numberOfImages = 1;
			using BinaryWriter writer = new BinaryWriter(File.Create(destination));
			writer.Write((short)0x0); // Reserved. Must always be 0.
			writer.Write((short)0x1); // Specifies image type: 1 for icon (.ICO) image
			writer.Write((short)numberOfImages); // Specifies number of images in the file.

			writer.Write((byte)dpi.Size.Value.Width);
			writer.Write((byte)dpi.Size.Value.Height);
			writer.Write((byte)0x0); // Specifies number of colors in the color palette
			writer.Write((byte)0x0); // Reserved. Should be 0
			writer.Write((short)0x1); // Specifies color planes. Should be 0 or 1
			writer.Write((short)0x8); // Specifies bits per pixel.
			writer.Write((int)memoryStream.Length); // Specifies the size of the image's data in bytes

			int offset = 6 + (16 * numberOfImages); // + length of previous images
			writer.Write(offset); // Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file

			// write png data for each image
			memoryStream.CopyTo(writer.BaseStream);
			writer.Flush();

			return new ResizedImageInfo { Dpi = dpi, Filename = destination };
		}

		public static string SkiaColorWithoutAlpha(SKColor? skColor)
		{
			var result = skColor?.ToString() ?? "transparent";
			if (!result.StartsWith("#"))
				return result;

			// Getting everything after '#ff'
			result = result.Substring(3);
			return "#" + result;
		}
	}
}
