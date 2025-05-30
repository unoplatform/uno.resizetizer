﻿using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;

namespace Uno.Resizetizer;

internal abstract partial class SkiaSharpTools
{
    public static SkiaSharpTools Create(bool isVector, string filename, SKSize? baseSize, SKColor? backgroundColor, SKColor? tintColor, ILogger logger)
        => isVector
            ? new SkiaSharpSvgTools(filename, baseSize, backgroundColor, tintColor, logger) as SkiaSharpTools
            : new SkiaSharpBitmapTools(filename, baseSize, backgroundColor, tintColor, logger);

    public static SkiaSharpTools CreateImaginary(SKColor? backgroundColor, ILogger logger)
        => new SkiaSharpImaginaryTools(backgroundColor, logger);

    public SkiaSharpTools(ResizeImageInfo info, ILogger logger)
        : this(info.Filename, info.BaseSize, info.Color, info.TintColor, logger)
    {
    }

    public SkiaSharpTools(string filename, SKSize? baseSize, SKColor? backgroundColor, SKColor? tintColor, ILogger logger)
    {
        Logger = logger;
        Filename = filename;
        BaseSize = baseSize;
        BackgroundColor = backgroundColor;

        if (tintColor is SKColor tint)
        {
            Logger?.Log($"Detected a tint color of {tint}");

            Paint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateBlendMode(tint, SKBlendMode.SrcIn)
            };
        }
    }

    public string Filename { get; }

    public SKSize? BaseSize { get; }

    public SKColor? BackgroundColor { get; }

    public ILogger Logger { get; }

    public SKPaint Paint { get; }

    public void Resize(DpiPath dpi, string destination, double additionalScale = 1.0, bool dpiSizeIsAbsolute = false)
    {
        var sw = new Stopwatch();
        sw.Start();

        var originalSize = GetOriginalSize();
        var absoluteSize = dpiSizeIsAbsolute ? dpi.Size : null;
        var (scaledSize, scale) = GetScaledSize(originalSize, dpi, absoluteSize);
        var (canvasSize, _) = GetCanvasSize(dpi, null, this);

        using (var tempBitmap = new SKBitmap(canvasSize.Width, canvasSize.Height))
        {
            Draw(tempBitmap, additionalScale, originalSize, scale, scaledSize);
            Save(destination, tempBitmap);
        }

        sw.Stop();
        Logger?.Log($"Save Image took {sw.ElapsedMilliseconds}ms ({destination})");
    }

    public static (SKSizeI Scaled, SKSize Unscaled) GetCanvasSize(DpiPath dpi, SKSize? baseSize = null, SkiaSharpTools baseTools = null)
    {
        // if an explicit size was given by the type of image, use that
        if (dpi.Size is SKSize size)
        {
            var scale = (float)dpi.Scale;
            var scaled = new SKSizeI(
                (int)(size.Width * scale),
                (int)(size.Height * scale));
            return (scaled, size);
        }

        // if an explicit size was given in the csproj, use that
        if (baseSize is SKSize bs)
        {
            var scale = (float)dpi.Scale;
            var scaled = new SKSizeI(
                (int)(bs.Width * scale),
                (int)(bs.Height * scale));
            return (scaled, bs);
        }

        // try determine the best size based on the loaded image
        if (baseTools is not null)
        {
            var baseOriginalSize = baseTools.GetOriginalSize();
            var (baseScaledSize, _) = baseTools.GetScaledSize(baseOriginalSize, dpi.Scale);
            return (baseScaledSize, baseOriginalSize);
        }

        throw new InvalidOperationException("The canvas size cannot be calculated if there is no size to start from (DPI size, BaseSize or image size).");
    }

    void Draw(SKBitmap tempBitmap, double additionalScale, SKSize originalSize, float scale, SKSizeI scaledSize)
    {
        using var canvas = new SKCanvas(tempBitmap);

        var canvasSize = tempBitmap.Info.Size;

        // clear
        canvas.Clear(BackgroundColor ?? SKColors.Transparent);

        // center the drawing
        canvas.Translate(
            (canvasSize.Width - scaledSize.Width) / 2,
            (canvasSize.Height - scaledSize.Height) / 2);

        // apply initial scale to size the image to fit the canvas
        canvas.Scale(scale, scale);

        // apply additional user scaling
        if (additionalScale != 1.0)
        {
            var userFgScale = (float)additionalScale;

            // add the user scale to the main scale
            scale *= userFgScale;

            // work out the center as if the canvas was exactly the same size as the foreground
            var fgCenterX = originalSize.Width / 2;
            var fgCenterY = originalSize.Height / 2;

            // scale to the user scale, centering
            canvas.Scale(userFgScale, userFgScale, fgCenterX, fgCenterY);
        }

        // draw
        DrawUnscaled(canvas, scale);
    }

    void Save(string destination, SKBitmap tempBitmap)
    {
        using var stream = File.Create(destination);
        tempBitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
    }

    public abstract SKSize GetOriginalSize();

    public abstract void DrawUnscaled(SKCanvas canvas, float scale);

    public (SKSizeI, float) GetScaledSize(SKSize originalSize, DpiPath dpi, SKSize? absoluteSize = null) =>
        GetScaledSize(originalSize, dpi.Scale, absoluteSize ?? dpi.Size);

    public (SKSizeI, float) GetScaledSize(SKSize originalSize, decimal resizeRatio, SKSize? absoluteSize = null)
    {
        var sourceNominalWidth = (int)(absoluteSize?.Width ?? BaseSize?.Width ?? originalSize.Width);
        var sourceNominalHeight = (int)(absoluteSize?.Height ?? BaseSize?.Height ?? originalSize.Height);

        // Find the actual size of the image
        var sourceActualWidth = (double)originalSize.Width;
        var sourceActualHeight = (double)originalSize.Height;

        // Figure out what the ratio to convert the actual image size to the nominal size is
        var nominalRatio = Math.Min(
            sourceNominalWidth / sourceActualWidth,
            sourceNominalHeight / sourceActualHeight);

        // Multiply nominal ratio by the resize ratio to get our final ratio we actually adjust by
        var adjustRatio = nominalRatio * (double)resizeRatio;

        // Figure out our scaled width and height to make a new canvas for
        var scaledWidth = sourceActualWidth * adjustRatio;
        var scaledHeight = sourceActualHeight * adjustRatio;
        var scaledSize = new SKSizeI(
            (int)Math.Round(scaledWidth),
            (int)Math.Round(scaledHeight));

        return (scaledSize, (float)adjustRatio);
    }
}
