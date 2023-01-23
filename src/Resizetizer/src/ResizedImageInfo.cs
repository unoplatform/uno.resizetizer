using System.Diagnostics;

namespace Uno.Resizetizer
{
	[DebuggerDisplay("{FileName}")]
	internal class ResizedImageInfo
	{
		public string Filename { get; set; }
		public DpiPath Dpi { get; set; }
	}
}
