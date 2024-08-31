using System.Diagnostics;
using System.Text;
using Apollo.Service;
using CUE4Parse.Utils;
using Serilog;

namespace Apollo.Managers;

public static class VideoManager
{
    private static void MakeVideo(int degreeOfParallelism)
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

        Log.Information("Audio Files Count: {count}", audioFiles.Length);
        Log.Information("Image Files Count: {count}", imageFiles.Length);
        if (audioFiles.Length != imageFiles.Length)
            Log.Warning("The number of audio files do not match with the number of image files");

        Log.Information("Degree of Parallelism: {degree}", degreeOfParallelism);
        
        var counter = 1;
        Parallel.For(0, imageFiles.Length, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism }, i =>
        {
            var outputPath = Path.Combine(ApplicationService.VideosDirectory, Path.ChangeExtension(audioFiles[i], ".mp4"));
            var ffmpegProcess = Process.Start(new ProcessStartInfo
            {
                FileName = ffmpegPath.FullName,
                Arguments = $"-loop 1 -i \"{imageFiles[i]}\" -i \"{audioFiles[i]}\" -c:v libx264 -c:a aac -b:a 192k -shortest -pix_fmt yuv420p \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            ffmpegProcess?.WaitForExit();


            demuxer.Add($"file '{outputPath}'");
            Log.Information("Exported {name} ({counter})", outputPath, $"{counter}/{imageFiles.Length}");

            Interlocked.Increment(ref counter);
        });

        demuxer.Sort(new NaturalStringComparer());
        File.WriteAllLines(Path.Combine(ApplicationService.DataDirectory, "videos.txt"), demuxer);
    }

    public static void MakeFinalVideo(int degreeOfParallelism)
    {
        MakeVideo(degreeOfParallelism);
        
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
        Log.Information("Created the final video at {location}. Love Ghost, Asval and Lulu :)", outputPath);
    }
}