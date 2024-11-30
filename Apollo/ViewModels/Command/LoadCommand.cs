using System.Diagnostics;
using Apollo.Service;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;
using GenericReader;
using K4os.Compression.LZ4.Streams;
using Serilog;

namespace Apollo.ViewModels.Command;

public class LoadCommand
{
    public async Task LoadNewFiles(string backupPath, List<VfsEntry> entries)
    {
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

        var magic = archive.Read<uint>();
        if (magic != 0x504B4246)
        {
            archive.Position -= 4;
            while (archive.Position < archive.Length)
            {
                archive.Position += 29;
                paths[archive.ReadString().ToLower()[1..]] = 0;
                archive.Position += 4;
            }
        }
        else
        {
            var version = archive.Read<EBackupVersion>();
            var count = archive.Read<int>();
            for (var i = 0; i < count; i++)
            {
                archive.Position += 9;
                paths[archive.ReadString().ToLower()[1..]] = 0;
            }
        }

        foreach (var (key, value) in ApplicationService.CUE4Parse.Provider.Files)
        {
            if (value is not VfsEntry entry || paths.ContainsKey(key) || entry.Path.EndsWith(".uexp") ||
                entry.Path.EndsWith(".ubulk") || entry.Path.EndsWith(".uptnl")) continue;

            entries.Add(entry);
        }
        
        stopwatch.Stop();
        Log.Information("Loaded {files} new files", entries.Count);
    }
}

public enum EBackupVersion : byte
{
    BeforeVersionWasAdded = 0,
    Initial,

    LatestPlusOne,
    Latest = LatestPlusOne - 1
}
