using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Uno.Resizetizer;

public class RemoveSvgFromContentTask_v0 : Task
{
	[Required]
	public ITaskItem[] CollectionToRemove { get; set; } = Array.Empty<ITaskItem>();

	public ITaskItem[] UnoImages { get; set; } = [];

	[Output]
	public ITaskItem[] RemovedItems { get; set; } = Array.Empty<ITaskItem>();

	public override bool Execute()
	{
		try
		{
#if DEBUG_RESIZETIZER
			System.Diagnostics.Debugger.Launch();
#endif
			RemovedItems = RemoveUnoImagesSvgFromContent();
			return true;
		}
		catch (Exception ex)
		{
			Log.LogError($"error: {ex} message: {ex.Message}");
			return false;
		}
	}

	ITaskItem[] RemoveUnoImagesSvgFromContent()
	{
		var list = new List<ITaskItem>();
		foreach (var asset in CollectionToRemove)
		{
			var extension2 = Path.GetExtension(asset.ItemSpec) ?? string.Empty;
			var isSvg = extension2.Equals(".svg", StringComparison.OrdinalIgnoreCase);

			if (string.IsNullOrEmpty(extension2) || !isSvg)
			{
				continue;
			}

			var assetFullPath = asset.GetMetadata("fullpath");
			foreach (var unoImage in UnoImages)
			{
				var fullPath = unoImage.GetMetadata("fullpath");
				if (fullPath == assetFullPath)
				{
					list.Add(asset);
				}
			}
		}

		return list.ToArray();
	}
}