using System.IO.Compression;
using Apollo.Settings;
using Apollo.ViewModels;
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
        AudioFilesDirectory.Create();
        ImagesDirectory.Create();
        VideosDirectory.Create();
    }

    public static void Deinitialize()
    {
        AppSettings.Save();
    }

    public static async Task DownloadDependencies()
    {
        await InitBinkaDecoder().ConfigureAwait(false);
        await InitBackground().ConfigureAwait(false);
        await InitFont().ConfigureAwait(false);
    }

    private static async Task InitBinkaDecoder(bool forceDownload = true)
    {
        var binkadec = new FileInfo(Path.Combine(DataDirectory.FullName, "binkadec.exe"));
        if (File.Exists(binkadec.FullName) && forceDownload == false) return;

        await ApiVM.DownloadFileAsync("https://cdn.discordapp.com/attachments/1090611236045062175/1203262911838158919/binkadec.exe?ex=667691e9&is=66754069&hm=96dd7f96b296bde1aeb367dff1538218f1651bbd109ebb7972d4b463306cb19e&", binkadec.FullName);
        if (binkadec.Length > 0)
        {
            Log.Information("Successfully downloaded binka decoder at {dir}", binkadec.DirectoryName);
        }
        else
        {
            Log.Error("Couldn't download binka decoder");
        }
    }
    
    private static async Task InitBackground(bool forceDownload = true)
    {
        var background = new FileInfo(Path.Combine(DataDirectory.FullName, "background.png"));
        if (File.Exists(background.FullName) && forceDownload == false) return;

        await ApiVM.DownloadFileAsync("https://cdn.discordapp.com/attachments/1158404127102079083/1253657058897559634/New_Project.png?ex=6676a69e&is=6675551e&hm=033d6459db55cd966a6c5de785326b448ebadc44c8a804e9b89b9f91af326bb6&", background.FullName);
        if (background.Length > 0)
        {
            Log.Information("Successfully downloaded background image at {dir}", background.DirectoryName);
        }
        else
        {
            Log.Error("Couldn't download background image");
        }
    }

    private static async Task InitFont(bool forceDownload = true)
    {
        var font = new FileInfo(Path.Combine(DataDirectory.FullName, "BurbankBigCondensed-Bold.ttf"));
        if (File.Exists(font.FullName) && forceDownload == false) return;

        await ApiVM.DownloadFileAsync("https://github.com/4sval/FModel/raw/master/FModel/Resources/BurbankBigCondensed-Bold.ttf", font.FullName);
        if (font.Length > 0)
        {
            Log.Information("Successfully downloaded BurbankBigCondensed-Bold font in {dir}", font.DirectoryName);
        }
        else
        {
            Log.Error("Couldn't download BurbankBigCondensed-Bold font");
        }
    }
}