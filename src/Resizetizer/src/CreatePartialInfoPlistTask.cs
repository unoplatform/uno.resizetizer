using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Resizetizer
{
	public class CreatePartialInfoPlistTask_v0 : Task
	{
		[Required]
		public string IntermediateOutputPath { get; set; }

		public string PlistName { get; set; }

		public string Storyboard { get; set; }

		const string plistHeader =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>";
		const string plistFooter = @"
</dict>
</plist>";

		public override bool Execute()
		{
#if DEBUG_RESIZETIZER
		//	System.Diagnostics.Debugger.Launch();
#endif
			try
			{
				Directory.CreateDirectory(IntermediateOutputPath);

				var plistFilename = Path.Combine(IntermediateOutputPath, PlistName ?? "PartialInfo.plist");

				FileHelper.WriteFileIfChanged(
					plistFilename,
					Log,
					writer =>
					{
						writer.WriteLine(plistHeader);

						if (!string.IsNullOrEmpty(Storyboard))
						{
							writer.WriteLine("  <key>UILaunchStoryboardName</key>");
							writer.WriteLine($"  <string>{Path.GetFileNameWithoutExtension(Storyboard)}</string>");
						}

						writer.WriteLine(plistFooter);
					});
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
			}

			return !Log.HasLoggedErrors;
		}
	}
}