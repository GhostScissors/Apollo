using System.IO.Compression;
using Apollo.Settings;
using Apollo.ViewModels;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Apollo.Service;

public sealed class ApplicationService
{
    private static DirectoryInfo OutputDirectory = new(Path.Combine(Environment.CurrentDirectory, "Output"));
    public static DirectoryInfo DataDirectory = new(Path.Combine(OutputDirectory.FullName, ".data"));
    public static DirectoryInfo ManifestCacheDirectory = new(Path.Combine(DataDirectory.FullName, "ManifestCache"));
    public static DirectoryInfo ChunkCacheDirectory = new(Path.Combine(DataDirectory.FullName, "ChunksCache"));
    public static DirectoryInfo ExportDirectory = new(Path.Combine(OutputDirectory.FullName, "Export"));
    public static DirectoryInfo BinkaFiles = new(Path.Combine(ExportDirectory.FullName, "Binka"));
    public static DirectoryInfo WavFiles = new(Path.Combine(ExportDirectory.FullName, "Wav"));
    public static DirectoryInfo Images = new(Path.Combine(ExportDirectory.FullName, "Images"));
    public static DirectoryInfo Videos = new(Path.Combine(ExportDirectory.FullName, "Videos"));
    
    public static ApiEndpointViewModel ApiVM = new();
    public static CUE4ParseViewModel CUE4ParseVM = new();
    public static BackupViewModel BackupVM = new();
    public static SoundsViewModel SoundsVM = new();
    
    public static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .CreateLogger();

        AppSettings.Load();

        OutputDirectory.Create();
        DataDirectory.Create();
        ManifestCacheDirectory.Create();
        ChunkCacheDirectory.Create();
        ExportDirectory.Create();
        BinkaFiles.Create();
        WavFiles.Create();
        Images.Create();
        Videos.Create();
    }

    public static void Deinitialize()
    {
        AppSettings.Save();
    }

    public static async Task InitVgmStream()
    {
        var vgmZipFilePath = new FileInfo(Path.Combine(ApplicationService.DataDirectory.FullName, "vgmstream-win.zip"));
        if (File.Exists(vgmZipFilePath.FullName)) return;

        await ApiVM.DownloadFileAsync("https://github.com/vgmstream/vgmstream/releases/latest/download/vgmstream-win.zip", vgmZipFilePath.FullName);
        if (vgmZipFilePath.Length > 0)
        {
            var zipDir = Path.GetDirectoryName(vgmZipFilePath.FullName)!;
            await using var zipFs = File.OpenRead(vgmZipFilePath.FullName);
            using var zip = new ZipArchive(zipFs, ZipArchiveMode.Read);

            foreach (var entry in zip.Entries)
            {
                var entryPath = Path.Combine(zipDir, entry.FullName);
                await using var entryFs = File.Create(entryPath);
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(entryFs);
            }
        }
        else
        {
            Log.Error("Could not download VgmStream");
        }
    }
}