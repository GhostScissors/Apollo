﻿using System.Diagnostics;
using System.Text.RegularExpressions;
using Apollo.Enums;
using Apollo.Service;
using CUE4Parse.Compression;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using EpicManifestParser;
using EpicManifestParser.Api;
using GenericReader;
using K4os.Compression.LZ4.Streams;
using Serilog;

namespace Apollo.ViewModels;

public class CUE4ParseViewModel
{
    public StreamedFileProvider Provider { get; } = new("FortniteGame", true, new VersionContainer(EGame.GAME_UE5_5));
    public List<VfsEntry> NewEntries { get; } = [];

    public async Task Initialize(EUpdateMode updateMode)
    {
        ManifestInfo? manifestInfo;
        
        if (updateMode == EUpdateMode.UpdateMode)
            manifestInfo = await WatchForManifest().ConfigureAwait(false);
        else
            manifestInfo = await ApplicationService.ApiVM.EpicApi.GetManifestAsync().ConfigureAwait(false);

        Log.Information($"Downloading {manifestInfo?.Elements[0].BuildVersion}");
        var manifestOptions = new ManifestParseOptions
        {
            ManifestCacheDirectory = ApplicationService.ManifestCacheDirectory,
            ChunkCacheDirectory = ApplicationService.ChunkCacheDirectory,
            Zlibng = ZlibHelper.Instance,
            ChunkBaseUrl = "http://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/",
        };

        var (manifest, _) = await manifestInfo!.DownloadAndParseAsync(manifestOptions).ConfigureAwait(false);

        Parallel.ForEach(manifest.FileManifestList, fileManifest =>
        {
            if (fileManifest.FileName != "FortniteGame/Content/Paks/global.utoc" &&
                fileManifest.FileName != "FortniteGame/Content/Paks/pakchunk10-WindowsClient.utoc")
                return;

            // FFS ANNOYING SHIT SO I SKIDDED https://github.com/4sval/FModel/blob/dev/FModel/ViewModels/CUE4ParseViewModel.cs#L237C33-L238C169
            Provider.RegisterVfs(fileManifest.FileName, [fileManifest.GetStream()],
                it => new FStreamArchive(it, manifest.FileManifestList.First(x => x.FileName.Equals(it)).GetStream(),
                    Provider.Versions));

            Log.Information("Downloaded {fileName}", fileManifest.FileName);
        });

        foreach (var fileManifest in manifest.FileManifestList)
        {
            
        }

        await Provider.MountAsync().ConfigureAwait(false);
        await LoadMappings();
        await LoadNewFiles();
    }
    
    private async Task LoadMappings()
    {
        var mappings = await ApplicationService.ApiVM.FortniteCentralApi.GetMappingsAsync().ConfigureAwait(false);
        string mappingsPath;

        if (mappings?.Length <= 0)
        {
            Log.Warning("Response from FortniteCentral was invalid. Trying to find saved mappings");

            var savedMappings = new DirectoryInfo(ApplicationService.DataDirectory).GetFiles("*.usmap");
            if (savedMappings.Length <= 0)
            {
                Log.Error("Failed to find saved mappings");
                return;
            }

            mappingsPath = savedMappings.OrderBy(f => f.LastWriteTimeUtc).First().FullName;
        }
        else
        {
            Log.Information("Downloading {name}", mappings[0].FileName);
            mappingsPath = Path.Combine(ApplicationService.DataDirectory, mappings[0].FileName);
            await ApplicationService.ApiVM.DownloadFileAsync(mappings[0].Url, mappingsPath);
            Log.Information("Downloaded {name} at {path}", mappings[0].FileName, mappingsPath);
        }

        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
        Log.Information("Mappings pulled from {path}", mappingsPath);
    }
    
    private async Task LoadNewFiles()
    {
        await ApplicationService.BackupVM.DownloadBackup();
        var backupPath = ApplicationService.BackupVM.GetBackup();

        var stopwatch = Stopwatch.StartNew();

        await using var fileStream = new FileStream(backupPath, FileMode.Open);
        await using var memoryStream = new MemoryStream();
        using var reader = new GenericStreamReader(fileStream);

        if (reader.Read<uint>() == 0x184D2204u)
        {
            reader.Position -= 4;
            await using var compressionMethod = LZ4Stream.Decode(fileStream);
            await compressionMethod.CopyToAsync(memoryStream).ConfigureAwait(false);
        }
        else await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

        memoryStream.Position = 0;
        await using var archive = new FStreamArchive(fileStream.Name, memoryStream);

        var paths = new Dictionary<string, int>();
        while (archive.Position < archive.Length)
        {
            archive.Position += 29;
            paths[archive.ReadString().ToLower()[1..]] = 0;
            archive.Position += 4;
        }

        foreach (var (key, value) in Provider.Files)
        {
            if (value is not VfsEntry entry || paths.ContainsKey(key) || entry.Path.EndsWith(".uexp") ||
                entry.Path.EndsWith(".ubulk") || entry.Path.EndsWith(".uptnl")) continue;

            NewEntries.Add(entry);
        }
        
        stopwatch.Stop();
        Log.Information("Loaded {files} new files", NewEntries.Count);
    }
    
    private async Task<ManifestInfo?> WatchForManifest()
    {
        ManifestInfo newManifest;

        var initialManifest = await ApplicationService.ApiVM.EpicApi.GetManifestAsync();
        var initialVersion = initialManifest?.Elements[0].BuildVersion;

        while (true)
        {
            await Task.Delay(5000);

            Log.Information("Checking for an update. Current Build: {currentVersion}", initialVersion);

            newManifest = (await ApplicationService.ApiVM.EpicApi.GetManifestAsync().ConfigureAwait(false))!;
            var newVersion = newManifest.Elements[0].BuildVersion;

            if (initialVersion != newVersion)
                break;
        }

        Log.Information("New Update Detected! New Build: {newVersion}", newManifest.Elements[0].BuildVersion);
        return newManifest;
    }
}