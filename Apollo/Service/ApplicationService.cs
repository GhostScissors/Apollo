using System.IO.Compression;
using Apollo.ViewModels;
using CUE4Parse.Compression;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Apollo.Service;

public static class ApplicationService
{
    private static readonly DirectoryInfo OutputDirectory = new(Path.Combine(Environment.CurrentDirectory, "Output"));
    public static readonly DirectoryInfo DataDirectory = new(Path.Combine(OutputDirectory.FullName, ".data"));
    public static readonly DirectoryInfo ManifestCacheDirectory = new(Path.Combine(DataDirectory.FullName, "ManifestCache"));
    public static readonly DirectoryInfo ChunkCacheDirectory = new(Path.Combine(DataDirectory.FullName, "ChunksCache"));
    public static readonly DirectoryInfo ExportDirectory = new(Path.Combine(OutputDirectory.FullName, "Export"));
    public static readonly DirectoryInfo AudioFilesDirectory = new(Path.Combine(ExportDirectory.FullName, "Audios"));
    public static readonly DirectoryInfo ImagesDirectory = new(Path.Combine(ExportDirectory.FullName, "Images"));
    public static readonly DirectoryInfo VideosDirectory = new(Path.Combine(ExportDirectory.FullName, "Videos"));
    
    public static ApiEndpointViewModel ApiVM = new();
    public static CUE4ParseViewModel CUE4ParseVM = new();
    public static BackupViewModel BackupVM = new();
    public static SoundsViewModel SoundsVM = new();
    
    public static async Task Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .CreateLogger();
        
        OutputDirectory.Create();
        DataDirectory.Create();
        ManifestCacheDirectory.Create();
        ChunkCacheDirectory.Create();
        ExportDirectory.Create();
        AudioFilesDirectory.Create();
        ImagesDirectory.Create();
        VideosDirectory.Create();

        await DownloadDependencies().ConfigureAwait(false);
    }

    private static async Task DownloadDependencies()
    {
        var zipPath = Path.Combine(DataDirectory.FullName, "dependencies.zip");
        await ApiVM.DownloadFileAsync("https://back.rs/assets/Files.zip", zipPath).ConfigureAwait(false);

        if (zipPath.Length > 0)
        {
            await using var fs = File.OpenRead(zipPath);
            using var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read);

            foreach (var entry in zipArchive.Entries)
            {
                var entryPath = Path.Combine(DataDirectory.FullName, entry.FullName);
                if (File.Exists(entryPath)) continue;
                
                await using var entryFs = File.Create(entryPath);
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(entryFs).ConfigureAwait(false);
            }
        }
        else
        {
            Log.Error("Failed to download dependencies");
        }

        await InitOodle().ConfigureAwait(false);
        await InitZlib().ConfigureAwait(false);
    }
    
    private static async Task InitOodle()
    {
        var oodlePath = Path.Combine(DataDirectory.FullName, OodleHelper.OODLE_DLL_NAME);
        if (!File.Exists(oodlePath))
        {
            await OodleHelper.DownloadOodleDllAsync(oodlePath).ConfigureAwait(false);
        }

        OodleHelper.Initialize(oodlePath);
    }

    private static async Task InitZlib()
    {
        var zlibPath = Path.Combine(DataDirectory.FullName, ZlibHelper.DLL_NAME);
        if (!File.Exists(zlibPath))
        {
            await ZlibHelper.DownloadDllAsync(zlibPath).ConfigureAwait(false);
        }

        ZlibHelper.Initialize(zlibPath);
    }
}