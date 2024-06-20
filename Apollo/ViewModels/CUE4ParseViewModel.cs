using System.Diagnostics;
using System.Text.RegularExpressions;
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
    private readonly Regex _fortniteLive = new(@"^FortniteGame(/|\\)Content(/|\\)Paks(/|\\)",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    
    public StreamedFileProvider Provider { get; set; }
    public List<VfsEntry> NewEntries { get; set; }

    public CUE4ParseViewModel()
    {
        Provider = new StreamedFileProvider("FortniteGame", true, new(EGame.GAME_UE5_5));
        NewEntries = new List<VfsEntry>();
    }

    public async Task Initialize()
    {
        await InitOodle().ConfigureAwait(false);
        await InitZlib().ConfigureAwait(false);
        
        var manifestInfo = await WatchForManifest();

        Log.Information($"Downloading {manifestInfo?.Elements[0].BuildVersion}");
        var manifestOptions = new ManifestParseOptions
        {
            ManifestCacheDirectory = ApplicationService.ManifestCacheDirectory.FullName,
            ChunkCacheDirectory = ApplicationService.ChunkCacheDirectory.FullName,
            Zlibng = ZlibHelper.Instance,
            ChunkBaseUrl = "http://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/",
        };

        var (manifest, _) = await manifestInfo!.DownloadAndParseAsync(manifestOptions).ConfigureAwait(false);

        foreach (var fileManifest in manifest.FileManifestList)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (!_fortniteLive.IsMatch(fileManifest.FileName))
                continue;

            // FFS ANNOYING SHIT SO I SKIDDED https://github.com/4sval/FModel/blob/dev/FModel/ViewModels/CUE4ParseViewModel.cs#L237C33-L238C169
            Provider.RegisterVfs(fileManifest.FileName, [fileManifest.GetStream()],
                it => new FStreamArchive(it, manifest.FileManifestList.First(x => x.FileName.Equals(it)).GetStream(),
                    Provider.Versions));

            stopwatch.Stop();

            Log.Information("Downloaded {fileName} in {time} ms", fileManifest.FileName, stopwatch.ElapsedMilliseconds);
        }

        await Provider.MountAsync();
    }
    
    public async Task LoadMappings()
    {
        var mappings = await ApplicationService.ApiVM.FortniteCentralApi.GetMappingsAsync().ConfigureAwait(false);
        string mappingsPath;

        if (mappings!.Length <= 0)
        {
            Log.Warning("Response from FortniteCentral was invalid. Trying to find saved mappings");

            var savedMappings = new DirectoryInfo(ApplicationService.DataDirectory.FullName).GetFiles("*.usmap");
            if (savedMappings.Length <= 0)
            {
                Log.Error("Failed to find saved mappings");
                return;
            }

            mappingsPath = savedMappings.OrderBy(f => f.LastWriteTimeUtc).First().FullName;
            Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
        }
        else
        {
            Log.Information("Downloading {name}", mappings[0].FileName);
            mappingsPath = Path.Combine(ApplicationService.DataDirectory.FullName, mappings[0].FileName);
            await ApplicationService.ApiVM.DownloadFileAsync(mappings[0].Url, mappingsPath);
            Log.Information("Downloaded {name} at {path}", mappings[0].FileName, mappingsPath);
            Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
        }

        Log.Information("Mappings pulled from {path}", mappingsPath);
    }
    
    public async Task LoadNewFiles()
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
        Log.Information("Loaded {files} in {time} ms", NewEntries.Count, stopwatch.ElapsedMilliseconds);
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

#if DEBUG
            if (initialVersion == newVersion)
                break;
#endif
            if (initialVersion != newVersion)
                break;
        }

        Log.Information("New Update Detected! New Build: {newVersion}", newManifest.Elements[0].BuildVersion);
        return newManifest;
    }
    
    private async Task InitOodle()
    {
        var oodlePath = Path.Combine(ApplicationService.DataDirectory.FullName, OodleHelper.OODLE_DLL_NAME);
        if (!File.Exists(oodlePath))
        {
            await OodleHelper.DownloadOodleDllAsync(oodlePath);
        }

        OodleHelper.Initialize(oodlePath);
    }

    private async Task InitZlib()
    {
        var zlibPath = Path.Combine(ApplicationService.DataDirectory.FullName, ZlibHelper.DLL_NAME);
        if (!File.Exists(zlibPath))
        {
            await ZlibHelper.DownloadDllAsync(zlibPath);
        }

        ZlibHelper.Initialize(zlibPath);
    }
}