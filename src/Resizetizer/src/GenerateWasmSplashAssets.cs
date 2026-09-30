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

		//if (System.Diagnostics.Debugger.IsAttached)
		//{
		//	System.Diagnostics.Debugger.Break();
		//}
		//System.Diagnostics.Debugger.Launch();

#endif
		if (UnoSplashScreen is null || UnoSplashScreen.Length is 0 )
		{
			Log.LogMessage(MessageImportance.Low, "No UnoSplashScreen item is configured; skipping WebAssembly splash screen generation.");
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
			// Log an error, not a warning: this method returns false, and MSBuild treats "task returned
			// false without logging an error" as the opaque MSB4181. A clear message tells the user how to fix it.
			Log.LogError("A WebAssembly splash screen is configured (a UnoSplashScreen item is present), but the required AppManifest.js embedded resource was not found. To generate the splash screen, add an AppManifest.js at Platforms/WebAssembly/WasmScripts/AppManifest.js (see https://platform.uno/docs/articles/wasm-appmanifest.html for its expected contents). If a WebAssembly splash screen is not needed, remove the UnoSplashScreen item instead.");
			return false;
		}

		var dir = Path.GetDirectoryName(OutputFile);
		Directory.CreateDirectory(dir);

		FileHelper.WriteFileIfChanged(
			OutputFile,
			Log,
			writer => ProcessAppManifestFile(UserAppManifest.ToString(), info, writer));

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
