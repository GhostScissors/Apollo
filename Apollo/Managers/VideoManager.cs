using System.Diagnostics;
using System.Text;
using Apollo.Service;
using Serilog;

namespace Apollo.Managers;

public static class VideoManager
{
    private static void MakeVideo()
    {
        var demuxer = new List<string>();
        var audioFiles = Directory.GetFiles(ApplicationService.AudioFilesDirectory.FullName, "*.wav", SearchOption.AllDirectories);
        var imageFiles = Directory.GetFiles(ApplicationService.ImagesDirectory.FullName, "*.png", SearchOption.AllDirectories);

        var ffmpegPath = new FileInfo(Path.Combine(ApplicationService.DataDirectory.FullName, "ffmpeg.exe"));
        if (!File.Exists(ffmpegPath.FullName))
        {
            Log.Error("{name} not present in .data directory", ffmpegPath.Name);
            return;
        }
        
        for (var i = 0; i < imageFiles.Length; i++)
        {
            var outputPath = Path.Combine(ApplicationService.VideosDirectory.FullName, Path.ChangeExtension(audioFiles[i], ".mp4"));
            var ffmpegProcess = Process.Start(new ProcessStartInfo
            {
                FileName = ffmpegPath.FullName,
                Arguments = $"-loop 1 -i \"{imageFiles[i]}\" -i \"{audioFiles[i]}\" -c:v libx264 -c:a aac -b:a 192k -shortest -pix_fmt yuv420p \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            ffmpegProcess?.WaitForExit(5000);

            var counter = $"{i + 1}/{imageFiles.Length}";
            demuxer.Add($"file '{outputPath}'");
            Log.Information("Exported {name} ({howMany})", outputPath, counter);
        }
        
        demuxer.Sort((x, y) => x.CompareTo(y));
        File.WriteAllLines(Path.Combine(ApplicationService.DataDirectory.FullName, "videos.txt"), demuxer);
    }

    public static void MakeFinalVideo()
    {
        MakeVideo();
        
        var txtPath = Path.Combine(ApplicationService.DataDirectory.FullName, "videos.txt");
        var outputPath = Path.Combine(ApplicationService.ExportDirectory.FullName, "output.mp4");
        var ffmpegProcess = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(ApplicationService.DataDirectory.FullName, "ffmpeg.exe"),
            Arguments = $"-f concat -safe 0 -i \"{txtPath}\" -c copy \"{outputPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        ffmpegProcess?.WaitForExit(5000);
        
        Log.Information("Check ur output folder. Love Ghost and Lulu :)");
    }
}