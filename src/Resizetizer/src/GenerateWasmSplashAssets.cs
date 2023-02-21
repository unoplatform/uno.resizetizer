using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using System.Linq;

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

		var appManifestJsPath = EmbeddedResources.FirstOrDefault(x => x.ToString().EndsWith(".js"));

		if (appManifestJsPath is null)
		{
			Log.LogWarning("Didn't find AppManifest.js file.");
			return false;
		}

		var dir = Path.GetDirectoryName(OutputFile);
		Directory.CreateDirectory(dir);

		using var writer = File.CreateText(OutputFile);

		ProcessAppManifestFile(appManifestJsPath.ToString(), info, writer);

		return true;
	}


	void ProcessAppManifestFile(in string appManifestPath, ResizeImageInfo info, StreamWriter writer)
	{
		using var reader = new StreamReader(File.OpenRead(appManifestPath));

		while (!reader.EndOfStream)
		{
			var line = reader.ReadLine();
			if (line.TrimStart().StartsWith("splashScreenImage:"))
				line = ProcessLine(line, info);

			writer.WriteLine(line);
		}
	}

	static string ProcessLine(in string line, ResizeImageInfo info)
	{
		var newLine = $"    splashScreenImage: \"{info.OutputPath}/{info.OutputName}.png\",";
		return line.Replace(line, newLine);
	}
}
