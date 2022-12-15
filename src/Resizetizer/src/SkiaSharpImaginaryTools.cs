using System;
using SkiaSharp;

namespace Uno.Resizetizer
{
	internal class SkiaSharpImaginaryTools : SkiaSharpTools, IDisposable
	{
		public SkiaSharpImaginaryTools(ResizeImageInfo info, ILogger logger)
			: this(info.Color, logger)
		{
		}

		public SkiaSharpImaginaryTools(SKColor? backgroundColor, ILogger logger)
			: base(null, new SKSize(1, 1), backgroundColor, null, logger)
		{
		}

		public override SKSize GetOriginalSize() => BaseSize.Value;

		public override void DrawUnscaled(SKCanvas canvas, float scale)
		{
			// no-op
		}

		public void Dispose()
		{
		}
	}
}
