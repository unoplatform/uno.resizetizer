using System.IO;
using SkiaSharp;

namespace Uno.Resizetizer
{
	/// <summary>
	/// Generates a .ico file for the image.
	/// </summary>
	internal class WindowsIconGenerator
	{
		public WindowsIconGenerator(ResizeImageInfo info, string intermediateOutputPath, ILogger logger)
		{
			Info = info;
			Logger = logger;
			IntermediateOutputPath = intermediateOutputPath;
		}

		public ResizeImageInfo Info { get; private set; }
		public string IntermediateOutputPath { get; private set; }
		public ILogger Logger { get; private set; }

		public ResizedImageInfo Generate() => Utils.GenerateIcoFile(IntermediateOutputPath, Logger, Info);
	}
}
