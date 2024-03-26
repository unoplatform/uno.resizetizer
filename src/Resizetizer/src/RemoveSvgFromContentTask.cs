using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Uno.Resizetizer;

public class RemoveSvgFromContentTask_v0 : Task
{
	[Required]
	public ITaskItem[] CollectionToRemove { get; set; } = Array.Empty<ITaskItem>();

	[Output]
	public ITaskItem[] RemovedItems { get; set; } = Array.Empty<ITaskItem>();

	public override bool Execute()
	{
		try
		{
#if DEBUG_RESIZETIZER
			System.Diagnostics.Debugger.Launch();
#endif
			RemovedItems = RemoveDuplicateSvgAndPngFiles(CollectionToRemove);
			return true;
		}
		catch (Exception ex)
		{
			Log.LogError($"error: {ex} message: {ex.Message}");
			return false;
		}
	}

	static ITaskItem[] RemoveDuplicateSvgAndPngFiles(ITaskItem[] assets)
	{
		var list = new List<ITaskItem>();
		var count = assets.Length;
		for (var i = 0; i < count; i++)
		{
			var item = assets[i].ItemSpec;
			var extension = Path.GetExtension(item) ?? string.Empty;
			if (!extension.Equals(".svg", StringComparison.CurrentCultureIgnoreCase))
			{
				continue;
			}
			var svgFileName = Path.GetFileNameWithoutExtension(item);

			foreach (var item2 in assets)
			{
				var extension2 = Path.GetExtension(item2.ItemSpec) ?? string.Empty;
				if (extension2.Equals(".svg", StringComparison.CurrentCultureIgnoreCase)
					|| !extension2.Equals(".png", StringComparison.CurrentCultureIgnoreCase))
				{
					continue;
				}

				if (svgFileName == Path.GetFileNameWithoutExtension(item2.ItemSpec))
				{
					list.Add(assets[i]);
					assets[i] = null;
				}
			}
		}

		return list.ToArray();
	}
}