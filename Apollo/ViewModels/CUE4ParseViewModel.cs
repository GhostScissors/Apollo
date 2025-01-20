using System.Text.RegularExpressions;
using Apollo.Enums;
using Apollo.Service;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using EpicManifestParser;
using EpicManifestParser.Api;
using Serilog;

namespace Apollo.ViewModels;

public class CUE4ParseViewModel
{
    public readonly StreamedFileProvider Provider;
    public readonly List<VfsEntry> Entries;
    private ManifestInfo _manifestInfo;
    
    private readonly Regex _fortniteLive = new(@"^FortniteGame[/\\]Content[/\\]Paks[/\\]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public CUE4ParseViewModel()
    {
        Provider = new StreamedFileProvider("FortniteGame", true, new VersionContainer(EGame.GAME_UE5_5));
        Entries = [];
        _manifestInfo = new ManifestInfo
        {
            Elements = []
        };
    }

    public async Task InitializeAsync(EUpdateMode updateMode)
    {
        await GetManifestAsync(updateMode).ConfigureAwait(false);

        Log.Information("Downloading {ver}", _manifestInfo.Elements[0].BuildVersion);
        var manifestOptions = new ManifestParseOptions
        {
            ManifestCacheDirectory = ApplicationService.ManifestCacheDirectory,
            ChunkCacheDirectory = ApplicationService.ChunkCacheDirectory,
            Decompressor = ManifestZlibStreamDecompressor.Decompress,
            DecompressorState = ZlibHelper.Instance,
            ChunkBaseUrl = "http://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/",
        };

        var (manifest, _) = await _manifestInfo.DownloadAndParseAsync(manifestOptions).ConfigureAwait(false);
        
        Parallel.ForEach(manifest.Files, fileManifest =>
        {
            if (!_fortniteLive.IsMatch(fileManifest.FileName))
                return;

            Provider.RegisterVfs(fileManifest.FileName, [fileManifest.GetStream()],
                it => new FRandomAccessStreamArchive(it, manifest.Files.First(x => x.FileName.Equals(it)).GetStream(),
                    Provider.Versions));

            Log.Information("Downloaded {fileName}", fileManifest.FileName);
        });
        
        Provider.Initialize();
        await Provider.MountAsync();
        
        await LoadMappingsAsync().ConfigureAwait(false);
        await ApplicationService.Backup.LoadNewFilesAsync(Entries).ConfigureAwait(false);
    }

    private async Task LoadMappingsAsync()
    {
        var mappings = await ApplicationService.Api.FortniteCentralApi.GetMappingsAsync().ConfigureAwait(false);
        string mappingsPath;
        
        if (mappings.Length <= 0)
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
            await ApplicationService.Api.DownloadFileAsync(mappings[0].Url, mappingsPath);
            Log.Information("Downloaded {name} at {path}", mappings[0].FileName, mappingsPath);
        }

        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
        Log.Information("Mappings pulled from {path}", mappingsPath);
    }
    
    private async Task GetManifestAsync(EUpdateMode updateMode)
    {
        var initialManifest = await ApplicationService.Api.EpicApi.GetManifestAsync();
        var initialVersion = initialManifest.Elements[0].BuildVersion;

        if (updateMode == EUpdateMode.GetNewFiles)
        {
            _manifestInfo = initialManifest;
            return;
        }

        while (true)
        {
            await Task.Delay(2500);

            Log.Information("Checking for an update. Current Build: {currentVersion}", initialVersion);

            _manifestInfo = await ApplicationService.Api.EpicApi.GetManifestAsync().ConfigureAwait(false);
            var newVersion = _manifestInfo.Elements[0].BuildVersion;

            if (initialVersion == newVersion) 
                continue;
            
            Log.Information("New Update Detected! New Build: {newVersion}", _manifestInfo.Elements[0].BuildVersion);
        }
    }
}
