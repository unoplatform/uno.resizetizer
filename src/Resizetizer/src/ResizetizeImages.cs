﻿#nullable enable
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Uno.Resizetizer
{
	public class ResizetizeImages_v0 : UnoAsyncTask, ILogger
	{
		[Required]
		public string PlatformType { get; set; } = "android";

		[Required] public string IntermediateOutputPath { get; set; } = string.Empty;

		[Required] public string IntermediateOutputIconPath { get; set; } = string.Empty;

		public string PWAManifestPath { get; set; } = string.Empty;

		public string[]? InputsFile { get; set; }

		public string? TargetFramework { get; set; }

		internal static string? _TargetFramework { get; set; }

		internal static string? TargetPlatform { get; private set; }

		public ITaskItem[]? Images { get; set; }

		[Output]
		public ITaskItem[]? CopiedResources { get; set; }

		[Output]
		public ITaskItem? GeneratedIconPath { get; set; }

		[Output]
		public ITaskItem[]? AndroidAppIcons { get; set; }

		[Output]
		public string? PwaGeneratedManifestPath { get; set; }

		public string? IsMacEnabled { get; set; }

		public ILogger Logger => this;

		public override System.Threading.Tasks.Task ExecuteAsync()
		{
#if DEBUG_RESIZETIZER

		//if (System.Diagnostics.Debugger.IsAttached)
		//{
		//	System.Diagnostics.Debugger.Break();
		//}
		//System.Diagnostics.Debugger.Launch();

#endif
			TargetPlatform = PlatformType;
			_TargetFramework = TargetFramework;

			if (PlatformType is null)
			{
				Log.LogWarning("PlatformType is null.");
			}

			var images = ResizeImageInfo.Parse(Images);

			var dpis = DpiPath.GetDpis();

			if (dpis == null || dpis.Length <= 0)
			{
				return System.Threading.Tasks.Task.CompletedTask;
			}

			if (InputsFile is null || InputsFile.Length <= 0)
			{
				LogWarning("No input files detected, try to rebuild your project to fix it.");
				return System.Threading.Tasks.Task.CompletedTask;
			}

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
					LogWarning("Uno.Resizetizer0000", ex.ToString());

					throw;
				}
			});

			var copiedResources = new List<TaskItem>();

			foreach (var img in resizedImages)
			{
				var attr = new Dictionary<string, string>();
				string itemSpec = Path.GetFullPath(img.Filename);

				// Fix the item spec to be relative for mac
				if (bool.TryParse(IsMacEnabled, out bool isMac) && isMac)
				{
					itemSpec = img.Filename;
				}

				// Add DPI info to the itemspec so we can use it in the targets
				attr.Add("_ResizetizerDpiPath", img.Dpi.Path);
				attr.Add("_ResizetizerDpiScale", img.Dpi.Scale.ToString("0.0", CultureInfo.InvariantCulture));

				var taskItem = new TaskItem(itemSpec, attr);

				copiedResources.Add(taskItem);
			}

			CopiedResources = [.. copiedResources];

			return System.Threading.Tasks.Task.CompletedTask;
		}

		void ProcessAppIcon(ResizeImageInfo img, ConcurrentBag<ResizedImageInfo> resizedImages)
		{
			string appIconName = img.OutputName;

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
				{
					androidAppIcons.Add(new TaskItem(iconGenerated.Filename));
				}

				AndroidAppIcons = [.. androidAppIcons];
			}
			else if (PlatformType == "ios" ||
			         (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && "desktop".Equals(TargetFramework, StringComparison.InvariantCultureIgnoreCase)))
			{
				var logMessage = "desktop".Equals(TargetFramework, StringComparison.InvariantCultureIgnoreCase) ?
					"MacOS Icon Assets Generator" :
					"iOS Icon Assets Generator";

				LogDebugMessage(logMessage);

				var appleAssetGen = new AppleIconAssetsGenerator(img, appIconName, IntermediateOutputPath, appIconDpis, this);

				IEnumerable<ResizedImageInfo> assetsGenerated = appleAssetGen.Generate();

				foreach (ResizedImageInfo assetGenerated in assetsGenerated)
				{
					resizedImages.Add(assetGenerated);
				}
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

				var wasmIconGen = new WasmIconGenerator(img, IntermediateOutputIconPath, this, PWAManifestPath, appIconDpis);

				var icon = wasmIconGen.Generate();
				var manifestPath = wasmIconGen.ProcessThePwaManifest();
				PwaGeneratedManifestPath = manifestPath;

				string itemSpec = Path.GetFullPath(icon.Filename);
				GeneratedIconPath = new TaskItem(itemSpec);
			}

			LogDebugMessage($"Generating App Icon Bitmaps for DPIs");

			var appTool = new SkiaSharpAppIconTools(img, this);

			LogDebugMessage($"App Icon: Intermediate Path " + IntermediateOutputPath);
			var iconPath = GetIconPath(PlatformType);
			foreach (var dpi in appIconDpis)
			{
				LogDebugMessage($"App Icon: " + dpi);

				var destination = Resizer.GetFileDestination(img, dpi, iconPath)
					.Replace("{name}", appIconName);

				var (_, sourceModified) = Utils.FileExists(img.Filename);
				var (_, destinationModified) = Utils.FileExists(destination);

				LogDebugMessage($"App Icon Destination: " + destination);

				if (destinationModified > sourceModified)
				{
					Logger.Log($"Skipping `{img.Filename}` => `{destination}` file is up to date.");
					continue;
				}

				appTool.Resize(dpi, Path.ChangeExtension(destination, ".png"));
			}
		}

		string GetIconPath(string platformType) => platformType switch
		{
			"wasm" or "android" => IntermediateOutputIconPath,
			_ => IntermediateOutputPath
		};

		void ProcessImageResize(ResizeImageInfo img, DpiPath[] dpis, ConcurrentBag<ResizedImageInfo> resizedImages)
		{
			var resizer = new Resizer(img, IntermediateOutputPath, this);

			foreach (var dpi in dpis)
			{
				LogDebugMessage($"Resizing {img.Filename}");

				var r = resizer.Resize(dpi, GetInputFile(img.IsSplashScreen));
				resizedImages.Add(r);

				LogDebugMessage($"Resized {img.Filename}");
			}
		}

		void ProcessImageCopy(ResizeImageInfo img, DpiPath originalScaleDpi, ConcurrentBag<ResizedImageInfo> resizedImages)
		{
			var resizer = new Resizer(img, IntermediateOutputPath, this);

			LogDebugMessage($"Copying {img.Filename}");

			var r = resizer.CopyFile(originalScaleDpi, GetInputFile(img.IsSplashScreen));
			resizedImages.Add(r);

			LogDebugMessage($"Copied {img.Filename}");
		}

		void ILogger.Log(string message)
		{
			Log?.LogMessage(message);
		}

		string GetInputFile(bool isSplashScreen) => isSplashScreen switch
		{
			true => InputsFile![1],
			_ => InputsFile![0]
		};
	}
}
