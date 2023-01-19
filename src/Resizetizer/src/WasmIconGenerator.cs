using SkiaSharp;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Uno.Resizetizer;
internal sealed class WasmIconGenerator
{
	readonly string pwaManifestPath;
	readonly DpiPath[] dpiPaths;

	public ResizeImageInfo Info { get; private set; }
	public string IntermediateOutputPath { get; private set; }
	public ILogger Logger { get; private set; }

	public WasmIconGenerator(ResizeImageInfo info, string intermediateOutputPath, ILogger logger, string pWAManifestPath, DpiPath[] dpiPaths)
	{
		Info = info;
		IntermediateOutputPath = intermediateOutputPath;
		Logger = logger;
		this.pwaManifestPath = pWAManifestPath;
		this.dpiPaths = dpiPaths;
	}

	public ResizedImageInfo Generate()
	{
		string destinationFolder = IntermediateOutputPath;

		var fileName = "favicon";
		string destination = Path.Combine(destinationFolder, $"{fileName}.ico");
		Directory.CreateDirectory(destinationFolder);

		Logger.Log($"Generating ICO: {destination}");

		var tools = new SkiaSharpAppIconTools(Info, Logger);
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

		if (!string.IsNullOrWhiteSpace(pwaManifestPath))
			ProcessThePwaManifest();

		return new ResizedImageInfo { Dpi = dpi, Filename = destination };
	}

	void ProcessThePwaManifest()
	{
		var json = File.ReadAllText(pwaManifestPath);

		var jsonNodeManifest = JsonNode.Parse(json);

		if (!IconPropertyIsEmpty(jsonNodeManifest))
		{
			Logger.Log("The PWA manifest already contains an icons property, skipping the generation of the icons property.");
			return;
		}

		var appIconImagesJson = new JsonArray();

		foreach (var dpi in dpiPaths)
		{
			var w = dpi.Size.Value.Width.ToString("0.#", CultureInfo.InvariantCulture);
			var h = dpi.Size.Value.Height.ToString("0.#", CultureInfo.InvariantCulture);

			var fileName = Path.GetFileNameWithoutExtension(Info.OutputName);
			var imageIcon = new JsonObject
			{
				["src"] = $"{fileName}{dpi.ScaleSuffix}.png",
				["size"] = $"{w}x{h}",
				["type"] = "image/png",
			};

			appIconImagesJson.Add(imageIcon);
		}

		var jsonIconsObject = new JsonObject
		{
			["icons"] = appIconImagesJson,
		};

		var writeOptions = new JsonWriterOptions
		{
			Indented = true
		};

		var newPwaManifestName = Path.GetFileName(pwaManifestPath) + "Uno";
		var outputPath = Path.Combine(IntermediateOutputPath, newPwaManifestName);
		// Change this in the future
		using var fs = File.Create(outputPath);
		using var writer = new Utf8JsonWriter(fs, writeOptions);

		JsonHelper.Merge(json, jsonIconsObject.ToJsonString(), writer);
		
		writer.Flush();

		static bool IconPropertyIsEmpty(JsonNode node)
		{
			var value = node["icons"];

			if (value is null)
				return true;

			return !(value.AsArray().Count > 0);
		}
	}
}
