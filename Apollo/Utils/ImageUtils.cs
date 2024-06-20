using Apollo.Service;
using Serilog;
using SkiaSharp;

namespace Apollo.Utils;

public static class ImageUtils
{
    public static void MakeImage(string text, string fileName)
    {
        const string backgroundImagePath = @"D:\Programming\Apollo\Apollo\Resources\We Needs This.png";

        using (var backgroundBitmap = SKBitmap.Decode(backgroundImagePath))
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
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                TextAlign = SKTextAlign.Center,
                TextSize = 64
            };

            canvas.DrawText(text, 960F, 540F, paint);

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
        var lines = new System.Collections.Generic.List<string>();
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