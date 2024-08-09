using System.Diagnostics;
using System.Text;
using Apollo.Service;
using CUE4Parse.Utils;
using Serilog;

namespace Apollo.Managers;

public static class VideoManager
{
    private static void MakeVideo()
    {
        var demuxer = new List<string>();
        var audioFiles = Directory.GetFiles(ApplicationService.AudioFilesDirectory, "*.wav", SearchOption.AllDirectories);
        var imageFiles = Directory.GetFiles(ApplicationService.ImagesDirectory, "*.png", SearchOption.AllDirectories);

        var ffmpegPath = new FileInfo(Path.Combine(ApplicationService.DataDirectory, "ffmpeg.exe"));
        if (!File.Exists(ffmpegPath.FullName))
        {
            Log.Error("{name} not present in .data directory", ffmpegPath.Name);
            return;
        }
        
        for (var i = 0; i < imageFiles.Length; i++)
        {
            var ss = audioFiles[i].SubstringAfterLast($"{ApplicationService.AudioFilesDirectory}\\");
            var outputPath = Path.Combine(ApplicationService.VideosDirectory, Path.ChangeExtension(ss, "mp4"));
            var ffmpegProcess = Process.Start(new ProcessStartInfo
            {
                FileName = ffmpegPath.FullName,
                Arguments = $"-loop 1 -i \"{imageFiles[i]}\" -i \"{audioFiles[i]}\" -c:v libx264 -c:a aac -b:a 192k -shortest -pix_fmt yuv420p \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            ffmpegProcess?.WaitForExit(5000);
            
            demuxer.Add($"file '{outputPath}'");
            Log.Information("Exported {name} ({counter})", outputPath, $"{i + 1}/{imageFiles.Length}");
        }

        demuxer.Sort((x, y) => string.Compare(x, y, StringComparison.Ordinal));
        File.WriteAllLines(Path.Combine(ApplicationService.DataDirectory, "videos.txt"), demuxer);
    }

    public static void MakeFinalVideo()
    {
        MakeVideo();
        
        var txtPath = Path.Combine(ApplicationService.DataDirectory, "videos.txt");
        var outputPath = Path.Combine(ApplicationService.ExportDirectory, "output.mp4");
        var ffmpegProcess = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(ApplicationService.DataDirectory, "ffmpeg.exe"),
            Arguments = $"-f concat -safe 0 -i \"{txtPath}\" -c copy \"{outputPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        ffmpegProcess?.WaitForExit();

#if !DEBUG
        File.Delete(txtPath);
#endif
        Log.Information("Created the final video at {location}. Love Ghost and Lulu :)", outputPath);
    }
}