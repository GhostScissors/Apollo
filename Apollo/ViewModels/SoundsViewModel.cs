using System.Diagnostics;
using Apollo.Managers;
using Apollo.Service;
using Apollo.Utils;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Serilog;

namespace Apollo.ViewModels;

public class SoundsViewModel
{
    public void ExportBinkaAudioFiles()
    {
        var soundSequences = ApplicationService.CUE4ParseVM.NewEntries.Where(x => x.Path.StartsWith("FortniteGame/Plugins/GameFeatures/BattlePassS30_Quests/Content/Audio/VO/SoundSequences/")).ToArray().OrderBy(entry => entry.Name);
        foreach (var soundSequence in soundSequences)
        {
            var dialogueUObject = ProviderUtils.LoadObject(soundSequence.Path + "." + soundSequence.NameWithoutExtension);
            if (dialogueUObject.TryGetValue(out FStructFallback[] soundSequencesData, "SoundSequenceData"))
            {
                foreach (var soundSequenceData in soundSequencesData)
                {
                    var (subtitles, voiceLines) = LoadDialogueWave(soundSequenceData);

                    if (voiceLines != null)
                    {
                        voiceLines.Decode(true, out string audioFormat, out byte[]? data);
                        var exportPath = new FileInfo(Path.Combine(ApplicationService.AudioFilesDirectory.FullName,
                            $"{voiceLines.Name}.{audioFormat}"));

                        File.WriteAllBytes(exportPath.FullName, data!);
                        Log.Information("Exported {name} in {dir}", voiceLines.Name, exportPath.DirectoryName);
                    }

                    if (!string.IsNullOrWhiteSpace(subtitles))
                        ImageManager.MakeImage(subtitles, voiceLines!.Name);
                }
            }
        };
    }
    
    private (string, USoundWave) LoadDialogueWave(FStructFallback struc)
    {
        if (struc.TryGetValue(out FPackageIndex sound, "Sound") &&
            sound.Name.StartsWith("VO_", StringComparison.OrdinalIgnoreCase) &&
            ProviderUtils.TryGetPackageIndexExport(sound, out USoundCue soundCue) &&
            ProviderUtils.TryGetPackageIndexExport(soundCue.FirstNode, out UObject soundNodeDialoguePlayer))
        {
            if (soundNodeDialoguePlayer.TryGetValue(out FStructFallback dialogueWaveParameter, "DialogueWaveParameter") &&
                dialogueWaveParameter.TryGetValue(out FPackageIndex dialogueWaveIndex, "DialogueWave") &&
                ProviderUtils.TryGetPackageIndexExport(dialogueWaveIndex, out UObject dialogueWave))
            {
                var subtitles = GetSpokenText(dialogueWave);
                var soundWave = GetSoundWave(dialogueWave);

                return (subtitles, soundWave);
            }
        }

        return (null, null)!;
    }

    private string GetSpokenText(UObject dialogueWave)
    {
        return dialogueWave.TryGetValue(out string spokenText, "SpokenText") ? spokenText : "No subtitles found";
    }

    private USoundWave GetSoundWave(UObject dialogueWave)
    {
        if (dialogueWave.TryGetValue(out FStructFallback[] contextMappings, "ContextMappings"))
        {
            if (contextMappings[0].TryGetValue(out FPackageIndex soundWaveIndex, "SoundWave") &&
                ProviderUtils.TryGetPackageIndexExport(soundWaveIndex, out USoundWave soundWave))
            {
                return soundWave;
            }
        }

        return null!;
    }

    public async Task DecodeBinkaToWav()
    {
        var binkaFiles = new DirectoryInfo(ApplicationService.AudioFilesDirectory.FullName).GetFiles("*.BINKA").OrderBy(f => f.LastWriteTime).ToList();
        
        var binkadecPath = new FileInfo(Path.Combine(ApplicationService.DataDirectory.FullName, "binkadec.exe"));
        if (!File.Exists(binkadecPath.FullName))
        {
            Log.Error("Binka Decoder doesn't exist in .data folder");
            return;
        }

        foreach (var binkaFile in binkaFiles)
        {
            var wavFilePath = new FileInfo(Path.Combine(ApplicationService.AudioFilesDirectory.FullName,
                binkaFile.Name.Replace(binkaFile.Extension, ".wav")));

            var binkadecProcess = Process.Start(new ProcessStartInfo
            {
                FileName = binkadecPath.FullName,
                Arguments = $"-i \"{binkaFile.FullName}\" -o \"{wavFilePath.FullName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            binkadecProcess?.WaitForExit(1000);

            File.Delete(binkaFile.FullName);

            Log.Information("Successfully converted {file1} to {file2}", binkaFile.Name, wavFilePath.Name);
        }
    }
}