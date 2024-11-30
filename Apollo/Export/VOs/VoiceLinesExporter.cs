using System.Diagnostics;
using System.Text.RegularExpressions;
using Apollo.Managers;
using Apollo.Service;
using Apollo.Utils;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.GameTypes.FN.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using Serilog;

namespace Apollo.Export.VOs;

public partial class VoiceLinesExporter : IExporter
{
    private VfsEntry[] SoundSequences;

    public VoiceLinesExporter()
    {
        SoundSequences = [];
    }
    
    public async Task ExportAsync()
    {
        SoundSequences = ApplicationService.CUE4Parse.Entries.Where(x => MyRegex().IsMatch(x.Path)).ToArray();
        Log.Information("Found {number} FortSoundSequences", SoundSequences.Length);
        foreach (var soundSequence in SoundSequences)
        {
            var uObject = await ProviderUtils.LoadObject(soundSequence.PathWithoutExtension + "." + soundSequence.NameWithoutExtension).ConfigureAwait(false);
            if (uObject.ExportType != "FortSoundSequence")
                continue;
            
            var soundSequenceObject = (UFortSoundSequence) uObject;
            
            for (var i = 0; i < soundSequenceObject.SoundSequenceData.Length; i++)
            {
                var soundSequenceData = soundSequenceObject.SoundSequenceData[i];

                if (!soundSequenceData.Sound.Name.StartsWith("VO_", StringComparison.OrdinalIgnoreCase) ||
                    !ProviderUtils.TryGetPackageIndexExport(soundSequenceData.Sound.FirstNode, out UObject soundNodeDialoguePlayer) ||
                    !soundNodeDialoguePlayer.TryGetValue(out FStructFallback dialogueWaveParameter, "DialogueWaveParameter") ||
                    !dialogueWaveParameter.TryGetValue(out FPackageIndex dialogueWaveIndex, "DialogueWave") ||
                    !ProviderUtils.TryGetPackageIndexExport(dialogueWaveIndex, out UObject dialogueWave)) continue;
                
                var voiceLines = GetSoundWave(dialogueWave);
                var subtitles = GetSpokenText(dialogueWave);

                if (voiceLines == null || subtitles == null) continue;
                voiceLines.Decode(true, out var audioFormat, out var data);

                if (data == null)
                    continue;
                
                var path = Path.Combine(ApplicationService.AudioFilesDirectory, soundSequence.NameWithoutExtension, $"{i}-{voiceLines.Name}.{audioFormat.ToLower()}");
                Directory.CreateDirectory(path.SubstringBeforeLast("\\"));

                await File.WriteAllBytesAsync(path, data).ConfigureAwait(false);
                Log.Information("Exported {0} at '{1}'", voiceLines.Name, path);

                ImageService.MakeImage(subtitles, soundSequence.NameWithoutExtension, $"{i}-{voiceLines.Name}");
            }
        }

        DecodeBinkaToWav();
        VideoManager.MakeFinalVideo(Environment.ProcessorCount / 4);
    }

    private string? GetSpokenText(UObject dialogueWave)
    {
        return dialogueWave.TryGetValue(out string spokenText, "SpokenText") ? spokenText : null;
    }

    private USoundWave? GetSoundWave(UObject dialogueWave)
    {
        if (!dialogueWave.TryGetValue(out FStructFallback[] contextMappings, "ContextMappings")) return null;
        if (contextMappings[0].TryGetValue(out FPackageIndex soundWaveIndex, "SoundWave") &&
            ProviderUtils.TryGetPackageIndexExport(soundWaveIndex, out USoundWave soundWave))
            return soundWave;

        return null;
    }
    
    private static void DecodeBinkaToWav()
    {
        var binkaFiles = Directory.GetFiles(ApplicationService.AudioFilesDirectory, "*.binka", SearchOption.AllDirectories);
        
        var binkadecPath = Path.Combine(ApplicationService.DataDirectory, "binkadec.exe");
        if (!File.Exists(binkadecPath))
        {
            Log.Error("Binka Decoder doesn't exist in .data folder");
            return;
        }

        foreach (var binkaFile in binkaFiles)
        {
            var wavFilePath = Path.ChangeExtension(Path.Combine(ApplicationService.AudioFilesDirectory, binkaFile), "wav");
            var binkadecProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = binkadecPath,
                Arguments = $"-i \"{binkaFile}\" -o \"{wavFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            binkadecProcess?.WaitForExit(5000);
            
            File.Delete(binkaFile);
            Log.Information("Successfully converted '{file1}' to .wav", binkaFile);
        }
    }

    [GeneratedRegex(@"^FortniteGame/Plugins/GameFeatures/[\w_]+/Content/Audio/VO/SoundSequences/")]
    private static partial Regex MyRegex();
}