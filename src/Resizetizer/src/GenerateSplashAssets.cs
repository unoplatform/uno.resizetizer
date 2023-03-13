using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SkiaSharp;

namespace Uno.Resizetizer
{
	/// <summary>
	/// Generates the splash assets file for Windows
	/// </summary>
	public class GenerateSplashAssets_v0 : Task, ILogger
	{
		[Required]
		public string IntermediateOutputPath { get; set; }

		[Required]
		public ITaskItem[] UnoSplashScreen { get; set; }

		public override bool Execute()
		{
#if DEBUG_RESIZETIZER
			System.Diagnostics.Debugger.Launch();
#endif
			var splash = UnoSplashScreen[0];

			var img = ResizeImageInfo.Parse(splash);

			Directory.CreateDirectory(IntermediateOutputPath);

			var appTool = new SkiaSharpAppIconTools(img, this);

			Log.LogMessage(MessageImportance.Low, $"Splash Screen: Intermediate Path " + IntermediateOutputPath);

			foreach (var dpi in DpiPath.Windows.SplashScreen)
			{
				Log.LogMessage(MessageImportance.Low, $"Splash Screen: " + dpi);

				var destination = Resizer.GetFileDestination(img, dpi, IntermediateOutputPath);

				Log.LogMessage(MessageImportance.Low, $"Splash Screen Destination: " + destination);

				appTool.Resize(dpi, Path.ChangeExtension(destination, ".png"));
			}

			return !Log.HasLoggedErrors;
		}

		void ILogger.Log(string message)
		{
			Log?.LogMessage(message);
		}
	}
}
