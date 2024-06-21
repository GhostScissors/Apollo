using Apollo.Service;
using Serilog;
using SkiaSharp;

namespace Apollo.Utils;

public static class ImageUtils
{
    public static void MakeImage(string text, string fileName)
    {
        FileInfo backgroundImage = new(Path.Combine(ApplicationService.DataDirectory.FullName, "background.png"));
        const string fontPath = @"D:\Programming\Apollo\Apollo\bin\Debug\net8.0\Output\.data\BurbankBigCondensed-Bold.ttf";

        var typeface = SKTypeface.FromFile(fontPath);
        if (typeface == null)
        {
            Log.Error("Failed to load custom font");
            return;
        }
        
        using (var backgroundBitmap = SKBitmap.Decode(backgroundImage.FullName))
        {
            if (backgroundBitmap == null)
            {
                Log.Error("Failed to load background image");
                return;
            }

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

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var exportPath = new FileInfo(Path.Combine(ApplicationService.Images.FullName, $"{fileName}.png"));
            
            File.WriteAllBytes(exportPath.FullName,data.ToArray());
            
            Log.Information("Exported {file} at {dir}", exportPath.Name, exportPath.FullName);
        }
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