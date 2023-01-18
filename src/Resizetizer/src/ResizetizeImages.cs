using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Resizetizer
{
	public class ResizetizeImages : MauiAsyncTask, ILogger
	{
		[Required]
		public string PlatformType { get; set; } = "android";

		[Required]
		public string IntermediateOutputPath { get; set; }

		public string IntermediateOutputIconPath { get; set; }

		public string InputsFile { get; set; }

		public ITaskItem[] Images { get; set; }

		[Output]
		public ITaskItem[] CopiedResources { get; set; }

		[Output]
		public ITaskItem GeneratedIconPath { get; set; }

		[Output]
		public ITaskItem[] AndroidAppIcons { get; set; }

		public string IsMacEnabled { get; set; }

		public ILogger Logger => this;

		public override System.Threading.Tasks.Task ExecuteAsync()
		{
			var images = ResizeImageInfo.Parse(Images);

			var dpis = DpiPath.GetDpis();

			if (dpis == null || dpis.Length <= 0)
				return System.Threading.Tasks.Task.CompletedTask;

			var originalScaleDpi = DpiPath.GetOriginal();

			var resizedImages = new ConcurrentBag<ResizedImageInfo>();

			this.ParallelForEach(images, img =>
			{
				try
				{
					var opStopwatch = new Stopwatch();
					opStopwatch.Start();

					string op;

					if (img.IsAppIcon)
					{
						// App icons are special
						ProcessAppIcon(img, resizedImages);

						op = "App Icon";
					}
					else
					{
						// By default we resize, but let's make sure
						if (img.Resize)
						{
							ProcessImageResize(img, dpis, resizedImages);

							op = "Resize";
						}
						else
						{
							// Otherwise just copy the thing over to the 1.0 scale
							ProcessImageCopy(img, originalScaleDpi, resizedImages);

							op = "Copy";
						}
					}

					opStopwatch.Stop();

					LogDebugMessage($"{op} took {opStopwatch.ElapsedMilliseconds}ms");
				}
				catch (Exception ex)
				{
					LogWarning("MAUI0000", ex.ToString());

					throw;
				}
			});

			if (PlatformType == "tizen")
			{
				var tizenResourceXmlGenerator = new TizenResourceXmlGenerator(IntermediateOutputPath, Logger);
				tizenResourceXmlGenerator.Generate();
			}

			var copiedResources = new List<TaskItem>();

			foreach (var img in resizedImages)
			{
				var attr = new Dictionary<string, string>();
				string itemSpec = Path.GetFullPath(img.Filename);

				// Fix the item spec to be relative for mac
				if (bool.TryParse(IsMacEnabled, out bool isMac) && isMac)
					itemSpec = img.Filename;

				// Add DPI info to the itemspec so we can use it in the targets
				attr.Add("_ResizetizerDpiPath", img.Dpi.Path);
				attr.Add("_ResizetizerDpiScale", img.Dpi.Scale.ToString("0.0", CultureInfo.InvariantCulture));

				var taskItem = new TaskItem(itemSpec, attr);

				copiedResources.Add(taskItem);
			}

			CopiedResources = copiedResources.ToArray();

			return System.Threading.Tasks.Task.CompletedTask;
		}

		void ProcessAppIcon(ResizeImageInfo img, ConcurrentBag<ResizedImageInfo> resizedImages)
		{
#if DEBUG_RESIZETIZER
			System.Diagnostics.Debugger.Launch();
#endif
			var appIconName = img.OutputName;

			// Generate the actual bitmap app icons themselves
			var appIconDpis = DpiPath.GetAppIconDpis(PlatformType, appIconName);

			LogDebugMessage($"App Icon");

			// Apple and Android have special additional files to generate for app icons
			if (PlatformType == "android")
			{
				LogDebugMessage($"Android Adaptive Icon Generator");

				appIconName = appIconName.ToLowerInvariant();

				var adaptiveIconGen = new AndroidAdaptiveIconGenerator(img, appIconName, IntermediateOutputIconPath, this);
				var iconsGenerated = adaptiveIconGen.Generate();

				// We don't need to add the icons to the ResizedImages, they're just for images (Content)
				var androidAppIcons = new List<TaskItem>();
				foreach (var iconGenerated in iconsGenerated)
					androidAppIcons.Add(new TaskItem(iconGenerated.Filename));

				AndroidAppIcons = androidAppIcons.ToArray();
			}
			else if (PlatformType == "ios")
			{
				LogDebugMessage($"iOS Icon Assets Generator");

				var appleAssetGen = new AppleIconAssetsGenerator(img, appIconName, IntermediateOutputPath, appIconDpis, this);

				var assetsGenerated = appleAssetGen.Generate();

				foreach (var assetGenerated in assetsGenerated)
					resizedImages.Add(assetGenerated);
			}
			else if (PlatformType == "uwp" || PlatformType == "wpf")
			{
				LogDebugMessage($"Windows Icon Generator");

				var windowsIconGen = new WindowsIconGenerator(img, IntermediateOutputPath, this);

				var icon = windowsIconGen.Generate();
				GeneratedIconPath = new TaskItem(icon.Filename);

				resizedImages.Add(icon);
			}
			else if (PlatformType == "wasm")
			{
				LogDebugMessage($"Wasm Icon Generator");

				var wasmIconGen = new WasmIconGenerator(img, IntermediateOutputIconPath, this);

				var icon = wasmIconGen.Generate();

				string itemSpec = Path.GetFullPath(icon.Filename);
				GeneratedIconPath = new TaskItem(itemSpec);
			}

			LogDebugMessage($"Generating App Icon Bitmaps for DPIs");

			var appTool = new SkiaSharpAppIconTools(img, this);

			LogDebugMessage($"App Icon: Intermediate Path " + IntermediateOutputPath);
			var iconPath = PlatformType == "android" ? IntermediateOutputIconPath : IntermediateOutputPath;
			foreach (var dpi in appIconDpis)
			{
				LogDebugMessage($"App Icon: " + dpi);

				var destination = Resizer.GetFileDestination(img, dpi, iconPath)
					.Replace("{name}", appIconName);

				LogDebugMessage($"App Icon Destination: " + destination);

				appTool.Resize(dpi, Path.ChangeExtension(destination, ".png"));
			}
		}

		void ProcessImageResize(ResizeImageInfo img, DpiPath[] dpis, ConcurrentBag<ResizedImageInfo> resizedImages)
		{
			var resizer = new Resizer(img, IntermediateOutputPath, this);

			foreach (var dpi in dpis)
			{
				LogDebugMessage($"Resizing {img.Filename}");

				var r = resizer.Resize(dpi, InputsFile);
				resizedImages.Add(r);

				LogDebugMessage($"Resized {img.Filename}");
			}
		}

		void ProcessImageCopy(ResizeImageInfo img, DpiPath originalScaleDpi, ConcurrentBag<ResizedImageInfo> resizedImages)
		{
			var resizer = new Resizer(img, IntermediateOutputPath, this);

			LogDebugMessage($"Copying {img.Filename}");

			var r = resizer.CopyFile(originalScaleDpi, InputsFile);
			resizedImages.Add(r);

			LogDebugMessage($"Copied {img.Filename}");
		}

		void ILogger.Log(string message)
		{
			Log?.LogMessage(message);
		}
	}
}
