﻿using System.IO.Compression;
using System.Reflection;
using Apollo.ViewModels;
using CUE4Parse.Compression;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Apollo.Service;

public static class ApplicationService
{
    private static string OutputDirectory = Path.Combine(Environment.CurrentDirectory, "Output");
    public static readonly string DataDirectory = Path.Combine(OutputDirectory, ".data");
    public static readonly string LogsDirectory = Path.Combine(OutputDirectory, "Logs");
    public static readonly string ManifestCacheDirectory = Path.Combine(DataDirectory, "ManifestCache");
    public static readonly string ChunkCacheDirectory = Path.Combine(DataDirectory, "ChunksCache");
    public static readonly string ExportDirectory = Path.Combine(OutputDirectory, "Export");
    public static readonly string AudioFilesDirectory = Path.Combine(ExportDirectory, "Audios");
    public static readonly string ImagesDirectory = Path.Combine(ExportDirectory, "Images");
    public static readonly string VideosDirectory = Path.Combine(ExportDirectory, "Videos");
    
    public static ApiEndpointViewModel ApiVM = new();
    public static CUE4ParseViewModel CUE4ParseVM = new();
    public static BackupViewModel BackupVM = new();
    public static SoundsViewModel SoundsVM = new();
    
    public static async Task Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(Path.Combine(LogsDirectory, $"Apollo-{DateTime.Now:dd-MM-yyyy}.log"))
            .CreateLogger();

        foreach (var directory in new[] { OutputDirectory, DataDirectory, ManifestCacheDirectory, ChunkCacheDirectory, ExportDirectory, AudioFilesDirectory, ImagesDirectory, VideosDirectory, LogsDirectory })
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
        
        var exportedFiles = Directory.GetFiles(ExportDirectory, "*.*", SearchOption.AllDirectories);
        foreach (var exportedFile in exportedFiles)
            File.Delete(exportedFile);

        await DownloadDependencies().ConfigureAwait(false);
    }

    private static async Task DownloadDependencies()
    {
        var zipPath = Path.Combine(DataDirectory, "dependencies.zip");
        await ApiVM.DownloadFileAsync("https://api.myna.lol/sharp/windows", zipPath).ConfigureAwait(false);

        if (zipPath.Length > 0)
        {
            await using var fs = File.OpenRead(zipPath);
            using var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read);

            foreach (var entry in zipArchive.Entries)
            {
                var entryPath = Path.Combine(DataDirectory, entry.FullName);
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

        await InitBackground().ConfigureAwait(false);
        await InitOodle().ConfigureAwait(false);
        await InitZlib().ConfigureAwait(false);
    }

    private static async Task InitBackground()
    {
        var resourceName = "Apollo.Resources.background.png";
        string outputPath = Path.Combine(DataDirectory, "background.png");
        
        var assembly = Assembly.GetExecutingAssembly();

        await using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
            throw new NullReferenceException("Resource not found");

        await using var fileStream = new FileStream(outputPath, FileMode.Create);
        await resourceStream.CopyToAsync(fileStream).ConfigureAwait(false);
    }
    
    private static async Task InitOodle()
    {
        var oodlePath = Path.Combine(DataDirectory, OodleHelper.OODLE_DLL_NAME);
        if (!File.Exists(oodlePath))
        {
            await OodleHelper.DownloadOodleDllAsync(oodlePath).ConfigureAwait(false);
        }

        OodleHelper.Initialize(oodlePath);
    }

    private static async Task InitZlib()
    {
        var zlibPath = Path.Combine(DataDirectory, ZlibHelper.DLL_NAME);
        if (!File.Exists(zlibPath))
        {
            await ZlibHelper.DownloadDllAsync(zlibPath).ConfigureAwait(false);
        }

        ZlibHelper.Initialize(zlibPath);
    }
}