﻿using System.Diagnostics;
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
        var soundSequences = ApplicationService.CUE4ParseVM.NewEntries.Where(x => x.Path.StartsWith("FortniteGame/Plugins/GameFeatures/BattlePassS30_Quests/Content/Audio/VO/SoundSequences/")).ToList().OrderBy(entry => entry.Name);
        foreach (var soundSequence in soundSequences)
        {
            var dialogueUObject = ProviderUtils.LoadAllObjects(soundSequence.Path);
            if (dialogueUObject.TryGetValue(out FStructFallback[] soundSequencesData, "SoundSequenceData"))
            {
                foreach (var soundSequenceData in soundSequencesData)
                {
                    var (subtitles, voiceLines) = LoadDialogueWave(soundSequenceData);

                    if (voiceLines != null)
                    {
                        voiceLines.Decode(true, out string audioFormat, out byte[]? data);
                        
                        var exportPath = new FileInfo(Path.Combine(ApplicationService.BinkaFiles.FullName, $"{voiceLines.Name}.{audioFormat}"));
                        
                        File.WriteAllBytes(exportPath.FullName, data!);
                        Log.Information("Exported {name} at {export dir}", voiceLines.Name, exportPath.FullName);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(subtitles))
                        ImageUtils.MakeImage(subtitles, voiceLines!.Name);
                }
            }
        }
    }
    
    private (string, USoundWave) LoadDialogueWave(FStructFallback struc)
    {
        if (struc.TryGetValue(out FPackageIndex sound, "Sound") &&
            sound.Name.StartsWith("VO_", StringComparison.OrdinalIgnoreCase) &&
            ProviderUtils.TryGetPackageIndexExport(sound, out USoundCue soundCue) &&
            ProviderUtils.TryGetPackageIndexExport(soundCue.FirstNode, out UObject soundNodeDialoguePlayer))
        {
            if (soundNodeDialoguePlayer.TryGetValue(out FStructFallback dialogueWaveParameter, "DialogueWaveParameter"))
            {
                if (dialogueWaveParameter.TryGetValue(out FPackageIndex dialogueWaveIndex, "DialogueWave") &&
                    ProviderUtils.TryGetPackageIndexExport(dialogueWaveIndex, out UObject dialogueWave))
                {
                    var subtitles = GetSpokenText(dialogueWave);
                    var fdf = GetSoundWave(dialogueWave);

                    return (subtitles, fdf);
                }
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

    public void ConvertBinkaToWav()
    {
        var binkaFiles = new DirectoryInfo(ApplicationService.BinkaFiles.FullName).GetFiles("*.BINKA").OrderBy(f => f.LastWriteTime).ToList();
        
        var binkadecPath = new FileInfo(Path.Combine(ApplicationService.DataDirectory.FullName, "binkadec.exe"));
        if (!File.Exists(binkadecPath.FullName))
        {
            Log.Error("Binka Decoder doesn't exist in .data folder");
            return;
        }

        foreach (var binkaFile in binkaFiles)
        {
            var wavFilePath = new FileInfo(Path.Combine(ApplicationService.WavFiles.FullName, binkaFile.Name.Replace(binkaFile.Extension, ".wav")));
            
            var binkadecPorcess = Process.Start(new ProcessStartInfo
            {
                FileName = binkadecPath.FullName,
                Arguments = $"-i \"{binkaFile.FullName}\" -o \"{wavFilePath.FullName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            binkadecPorcess?.WaitForExit(5000);
            
            Log.Information("Successfully converted {file1} to {file2}", binkaFile.Name, wavFilePath.Name);
        }
    }
}