using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Uno.Resizetizer;
public class GenerateWasmSplashAssets_v0 : Task
{
	[Required]
	public string IntermediateOutputPath { get; set; }

	[Required]
	public string OutputFile { get; set; }

	[Required]
	public ITaskItem[] UnoSplashScreen { get; set; }

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
		if (UnoSplashScreen is { Length: 0 })
		{
			Log.LogWarning("Didn't find UnoSplashScreen.");
			return true;
		}

		var splash = UnoSplashScreen[0];

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

		dic["splashScreenImage"] = $"\"{info.OutputName}.scale-200.png\"";
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
			sb.AppendLine($"    {key}: {value},");
		}
		sb.Append('}');

		var final = sb.ToString();

		writer.Write(final);
	}

	static Dictionary<string, string> FindWhatINeed(string fileToProcess)
	{
		var indexOfSymbol = fileToProcess.IndexOf('{');
		var indexOfSymbolClose = fileToProcess.IndexOf('}');
		var input = fileToProcess.Substring(++indexOfSymbol, indexOfSymbolClose - indexOfSymbol);

		var dictionary = (from pair in input.Split(',')
						  let component = pair.Split(':')
						  where component.Length == 2
						  select new { Key = component[0].Trim(), Value = component[1].Trim() })
					  .ToDictionary(x => x.Key, x => x.Value);

		return dictionary;
	}

	static string ProcessSplashScreenColor(ResizeImageInfo info)
	{
		var color = Utils.SkiaColorWithoutAlpha(info.Color);
		return $"\"{color}\"";
	}
}
