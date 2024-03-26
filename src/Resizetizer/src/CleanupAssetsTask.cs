using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Resizetizer;

public class CleanupAssetsTask_v0 : Task
{
	[Required]
	[Output]
	public ITaskItem[] UnoImagesCollection { get; set; } = Array.Empty<ITaskItem>();

	[Required]
	[Output]
	public ITaskItem[] ContentCollection { get; set; } = Array.Empty<ITaskItem>();

	// This is just available on Android target, because of that isn't Required
	[Output]
	public ITaskItem[] AndroidAssetCollection { get; set; } = Array.Empty<ITaskItem>();

	public override bool Execute()
	{
#if DEBUG_RESIZETIZER
			// System.Diagnostics.Debugger.Launch();
#endif
		try
		{
			ContentCollection = RemoveUnoImageFrom(UnoImagesCollection, ContentCollection);
			AndroidAssetCollection = RemoveUnoImageFrom(UnoImagesCollection, AndroidAssetCollection);

			return true;
		}
		catch (Exception ex)
		{
			Log.LogError($"error: {ex} message: {ex.Message}");
			return false;
		}
	}


	static ITaskItem[] RemoveUnoImageFrom(ITaskItem[] unoImages, ITaskItem[] assets)
	{
		var count = assets.Length;

		for (var i = 0; i < count; i++)
		{
			foreach (var unoImage in unoImages)
			{
				if (assets[i].ItemSpec == unoImage.ItemSpec)
				{
					assets[i] = null;
					break;
				}
			}
		}

		return assets.Where(x => x is not null).ToArray();
	}
}
