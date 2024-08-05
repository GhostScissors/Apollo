using System.Diagnostics;
using Apollo.Service;
using Serilog;

namespace Apollo.Managers;

public static class VideoManager
{
    private static void MakeVideo()
    {
        var audioFiles = new DirectoryInfo(ApplicationService.AudioFilesDirectory.FullName).GetFiles("*.wav").OrderBy(f => f.LastWriteTime).ToArray();
        var imageFiles = new DirectoryInfo(ApplicationService.ImagesDirectory.FullName).GetFiles("*.png").OrderBy(f => f.LastWriteTime).ToArray();

        var ffmpegPath = new FileInfo(Path.Combine(ApplicationService.DataDirectory.FullName, "ffmpeg.exe"));
        if (!File.Exists(ffmpegPath.FullName))
        {
            Log.Error("{name} not present in .data directory", ffmpegPath.Name);
            return;
        }

        for (var i = 0; i < imageFiles.Length; i++)
        {
            var outputPath = new FileInfo(Path.Combine(ApplicationService.VideosDirectory.FullName, audioFiles[i].Name.Replace(audioFiles[i].Extension, ".mp4")));
            
            var ffmpegProcess = Process.Start(new ProcessStartInfo
            {
                FileName = ffmpegPath.FullName,
                Arguments = $"-loop 1 -i \"{imageFiles[i].FullName}\" -i \"{audioFiles[i].FullName}\" -c:v libx264 -c:a aac -b:a 192k -shortest -pix_fmt yuv420p \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            ffmpegProcess?.WaitForExit(5000);

            var counter = $"{i + 1}/{imageFiles.Length}";
            Log.Information("Exported {name} at {dir} ({howMany})", outputPath.Name, outputPath.FullName, counter);
        }
    }

    public static void MakeFinalVideo()
    {
        MakeVideo();
        
        var videos = new DirectoryInfo(ApplicationService.VideosDirectory.FullName).GetFiles("*.mp4").OrderBy(f => f.LastWriteTime).ToArray();
        var ffmpegPath = new FileInfo(Path.Combine(ApplicationService.DataDirectory.FullName, "ffmpeg.exe"));
        var txtPath = Path.Combine(ApplicationService.DataDirectory.FullName, "videos.txt");
        var outputPath = new FileInfo(Path.Combine(Path.Combine(ApplicationService.ExportDirectory.FullName, "output.mp4")));
        
        var videoFileLines = videos.Select(video => $"file '{video.FullName}'").ToArray();
        File.WriteAllLines(txtPath, videoFileLines);
        
        var ffmpegProcess = Process.Start(new ProcessStartInfo
        {
            FileName = ffmpegPath.FullName,
            Arguments = $"-f concat -safe 0 -i \"{txtPath}\" -c copy \"{outputPath.FullName}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        ffmpegProcess?.WaitForExit(5000);
        
        Log.Information("Check ur output folder. Love Ghost and Lulu :)");
    }
}