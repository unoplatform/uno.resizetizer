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

	public ResizedImageInfo Generate() => Utils.GenerateIcoFile(IntermediateOutputPath, Logger, Info, "favicon");

	public string ProcessThePwaManifest()
	{
#if DEBUG_RESIZETIZER

		if (System.Diagnostics.Debugger.IsAttached)
		{
			System.Diagnostics.Debugger.Break();
		}
		System.Diagnostics.Debugger.Launch();

#endif
		if (string.IsNullOrWhiteSpace(pwaManifestPath))
		{
			Logger.Log("There's no PWA Manifest file.");
			return string.Empty;
		}

		var json = File.ReadAllText(pwaManifestPath);

		var jsonNodeManifest = JsonNode.Parse(json);

		if (!IconPropertyIsEmpty(jsonNodeManifest))
		{
			Logger.Log("The PWA manifest already contains an icons property, skipping the generation of the icons property.");
			return string.Empty;
		}

		var appIconImagesJson = new JsonArray();
		Logger.Log("Creating the icons property for the PWA manifest.");
		
		foreach (var dpi in dpiPaths)
		{
			var w = dpi.Size.Value.Width.ToString("0.#", CultureInfo.InvariantCulture);
			var h = dpi.Size.Value.Height.ToString("0.#", CultureInfo.InvariantCulture);

			var fileName = Path.GetFileNameWithoutExtension(Info.OutputName);
			var imageIcon = new JsonObject
			{
				["src"] = $"{fileName}{dpi.ScaleSuffix}.png",
				["sizes"] = $"{w}x{h}",
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

		Logger.Log("Writing the PWA manifest with the icons property.");
		var newPwaManifestName = "Uno" + Path.GetFileName(pwaManifestPath);
		var outputPath = Path.Combine(IntermediateOutputPath, newPwaManifestName);

		using var fs = File.Create(outputPath);
		using var writer = new Utf8JsonWriter(fs, writeOptions);

		Logger.Log("Merging the original PWA manifest with the icons property.");
		
		JsonHelper.Merge(json, jsonIconsObject.ToJsonString(), writer);

		writer.Flush();

		var path = Path.GetFullPath(outputPath);

		Logger.Log($"PWA manifest path: {path}.");

		return path;

		static bool IconPropertyIsEmpty(JsonNode node)
		{
			var value = node["icons"];

			if (value is null)
			{
				return true;
			}

			return !(value.AsArray().Count > 0);
		}
	}
}
