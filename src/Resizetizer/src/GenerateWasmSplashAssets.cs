using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uno.Resizetizer;
public class GenerateWasmSplashAssets : Task
{
	[Required]
	public string IntermediateOutputPath { get; set; }

	[Required]
	public string OutputFile { get; set; }

	[Required]
	public ITaskItem[] MauiSplashScreen { get; set; }

	[Required]
	public ITaskItem[] EmbeddedResources { get; set; }

	[Output]
	public ITaskItem UserAppManifest { get; set; }


	public override bool Execute()
	{
#if DEBUG_RESIZETIZER

		if (System.Diagnostics.Debugger.IsAttached)
		{
			System.Diagnostics.Debugger.Break();
		}
		System.Diagnostics.Debugger.Launch();

#endif
		if (MauiSplashScreen is null)
		{
			Log.LogWarning("Didn't find MauiSplashScreen.");
			return false;
		}

		var splash = MauiSplashScreen[0];

		var info = ResizeImageInfo.Parse(splash);

		UserAppManifest = EmbeddedResources.FirstOrDefault(x => 
		{
			var name = x.ToString();

			return name.EndsWith("AppManifest.js", StringComparison.OrdinalIgnoreCase)
			|| name.EndsWith("AppManifest", StringComparison.OrdinalIgnoreCase);
		});

		if (UserAppManifest is null)
		{
			Log.LogWarning("Didn't find AppManifest.js file.");
			return false;
		}

		var dir = Path.GetDirectoryName(OutputFile);
		Directory.CreateDirectory(dir);

		using var writer = File.CreateText(OutputFile);

		ProcessAppManifestFile(UserAppManifest.ToString(), info, writer);

		return true;
	}


	void ProcessAppManifestFile(in string appManifestPath, ResizeImageInfo info, StreamWriter writer)
	{
		using var reader = new StreamReader(File.OpenRead(appManifestPath));
		var fileToProcess = reader.ReadToEnd();

		var dic = FindWhatINeed(fileToProcess);

		dic["splashScreenImage"] = $"{info.OutputName}.scale-400.png";
		dic["splashScreenColor"] = ProcessSplashScreenColor(info);

		WriteToFile(dic, writer);
	}

	static void WriteToFile(Dictionary<string, string> dic, StreamWriter writer)
	{
		var sb = new StringBuilder(@"var UnoAppManifest = {").AppendLine();
		foreach (var kvp in dic)
		{
			var key = kvp.Key;
			var value = kvp.Value;
			sb.AppendLine($"    {key}: \"{value}\",");
		}
		sb.Append('}');

		var final = sb.ToString();

		writer.Write(final);
	}

	static Dictionary<string, string> FindWhatINeed(string fileToProcess)
	{
		var indexOfSymbol = fileToProcess.IndexOf('{');
		var input = fileToProcess.Substring(indexOfSymbol);
		// Remove whitespace and newlines from the input
		input = input.Replace(" ", "").Replace("\r", "").Replace("\n", "");

		// Remove the opening and closing braces from the input
		input = input.TrimStart('{').TrimEnd('}');

		var dictionary = new Dictionary<string, string>();

		// Split the input into key-value pairs
		var pairs = input.Split(',');

		foreach (var pair in pairs)
		{
			// Split each key-value pair into its components
			var components = pair.Split(':');

			// Remove the quotes from the key and value
			var key = components[0].Replace("\"", "");
			var value = components[1].Replace("\"", "");

			// Add the key-value pair to the dictionary
			dictionary.Add(key, value);
		}

		// Print the dictionary
		foreach (var pair in dictionary)
		{
			Console.WriteLine($"{pair.Key}: {pair.Value}");
		}

		return dictionary;
	}

	static string ProcessSplashScreenColor(ResizeImageInfo info)
	{
		var color = ColorWithoutAlpha(info.Color);
		return $"{color}";
	}

	// Wasm doesn't support alpha
	static string ColorWithoutAlpha(SKColor? color)
	{
		var result = color?.ToString() ?? "transparent";
		if (!result.StartsWith("#"))
			return result;

		// Getting everything after '#ff'
		result = result.Substring(3);
		return "#" + result;
	}
}
