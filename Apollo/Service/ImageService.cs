using CUE4Parse.Utils;
using Serilog;
using SkiaSharp;

namespace Apollo.Service;

public static class ImageService
{
    public static void MakeImage(string text, string folder, string fileName)
    {
        var backgroundImage = Path.Combine(ApplicationService.DataDirectory, "background.png");
        var fontPath = Path.Combine(ApplicationService.DataDirectory, "burbankbigcondensed_bold.otf");
        const string credits = "via - @GhostScissors_ & @Loolo_WRLD";

        var typeface = SKTypeface.FromFile(fontPath);
        if (typeface == null)
            throw new FileNotFoundException("Font file does not exist");

        using var backgroundBitmap = SKBitmap.Decode(backgroundImage);
        if (backgroundBitmap == null)
            throw new FileNotFoundException("Background file does not exist");

        using var surface = SKSurface.Create(new SKImageInfo(backgroundBitmap.Width, backgroundBitmap.Height));
        using var canvas = surface.Canvas;
            
        canvas.DrawBitmap(backgroundBitmap, 0, 0);
            
        var paint = new SKPaint
        {
            Typeface = typeface,
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            TextAlign = SKTextAlign.Center,
            TextSize = 129
        };
            
        var lines = SplitTextIntoLines(text, backgroundBitmap.Width - 20, paint);

        var totalTextHeight = lines.Length * paint.TextSize + (lines.Length - 1) * 10;
        var yStart = (backgroundBitmap.Height - totalTextHeight) / 2 + paint.TextSize;

        var x = 960;
        var y = yStart;

        foreach (var line in lines)
        {
            canvas.DrawText(line, x, y, paint);
            y += paint.TextSize + 10;
        }
            
        var smallTextPaint = new SKPaint
        {
            Typeface = typeface,
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            TextAlign = SKTextAlign.Right,
            TextSize = 64
        };
            
        canvas.DrawText(credits, 1900, backgroundBitmap.Height - 20, smallTextPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var exportPath = Path.Combine(ApplicationService.ImagesDirectory, folder, $"{fileName}.png");

        Directory.CreateDirectory(exportPath.SubstringBeforeLast("\\"));
        File.WriteAllBytesAsync(exportPath,data.ToArray());
        Log.Information("Exported {file}", exportPath);
    }
    
    private static string[] SplitTextIntoLines(string text, float maxWidth, SKPaint paint)
    {
        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = "";

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var textWidth = paint.MeasureText(testLine);

            if (textWidth > maxWidth)
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return lines.ToArray();
    }
}