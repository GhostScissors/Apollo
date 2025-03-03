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

        Parallel.ForEach(SoundSequences, soundSequence =>
        {
            var soundSequenceObject = ProviderUtils.LoadObject<UFortSoundSequence>(soundSequence.PathWithoutExtension + "." + soundSequence.NameWithoutExtension);
            
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

                File.WriteAllBytes(path, data);
                Log.Information("Exported {0} at '{1}'", voiceLines.Name, path);

                ImageService.MakeImage(subtitles, soundSequence.NameWithoutExtension, $"{i}-{voiceLines.Name}");
            }
        });

        DecodeRadaToWav();
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
    
    private static void DecodeRadaToWav()
    {
        var radaFiles = Directory.GetFiles(ApplicationService.AudioFilesDirectory, "*.rada", SearchOption.AllDirectories);
        
        var radadecPath = Path.Combine(ApplicationService.DataDirectory, "radadec.exe");
        if (!File.Exists(radadecPath))
        {
            Log.Error("RADA Decoder doesn't exist in .data folder");
            return;
        }

        foreach (var radaFile in radaFiles)
        {
            var wavFilePath = Path.ChangeExtension(Path.Combine(ApplicationService.AudioFilesDirectory, radaFile), "wav");
            var radaDecProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = radadecPath,
                Arguments = $"-i \"{radaFile}\" -o \"{wavFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            radaDecProcess?.WaitForExit(5000);
            
            File.Delete(radaFile);
            Log.Information("Successfully converted '{file1}' to .wav", radaFile);
        }
    }

    [GeneratedRegex(@"^FortniteGame/Plugins/GameFeatures/[\w_]+/Content/Audio/VO/SoundSequences/")]
    private static partial Regex MyRegex();
}