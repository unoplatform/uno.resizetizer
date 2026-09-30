using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Resizetizer
{
	public class DetectInvalidResourceOutputFilenamesTask_v0 : Task
	{
		public ITaskItem[] Items { get; set; }

		public bool ThrowsError { get; set; } = true;

		[Required]
		public string ErrorMessage { get; set; }

		[Output]
		public string[] InvalidItems { get; set; }

		public override bool Execute()
		{
			var invalidFilenames = new List<string>();
			var invalidNames = new List<string>();
			try
			{
				if (Items != null)
				{
					foreach (var item in Items)
					{
						// The generators derive the output name from %(Link) when it is set
						// (see ResizeImageInfo.OutputName), so validating the ItemSpec alone lets
						// a non-conforming Link through unchecked.
						var outputName = item.GetMetadata("Link");

						if (string.IsNullOrWhiteSpace(outputName))
						{
							outputName = item.ItemSpec;
						}

						if (!Utils.IsValidResourceFilename(outputName))
						{
							invalidFilenames.Add(item.ItemSpec);
							invalidNames.Add(Path.GetFileNameWithoutExtension(outputName));
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
			}
			finally
			{
				if (invalidFilenames.Count > 0)
				{
					InvalidItems = invalidFilenames.ToArray();

					var builder = new StringBuilder();
					builder.Append(ErrorMessage);

					for (var i = 0; i < invalidNames.Count; i++)
					{
						if (i > 0)
						{
							builder.Append(", ");
						}

						builder.Append(invalidNames[i]);
					}

					if (ThrowsError)
					{
						Log.LogError(builder.ToString());
					}
					else
					{
						Log.LogMessage(MessageImportance.High, builder.ToString());
					}
				}
			}

			return !Log.HasLoggedErrors;
		}
	}
}
