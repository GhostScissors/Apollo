﻿using System.Diagnostics;
using Apollo.Managers;
using Apollo.Service;
using Apollo.Utils;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.GameTypes.FN.Assets.Exports.Sound;
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
        var soundSequences = ApplicationService.CUE4ParseVM.NewEntries.Where(x => x.Path.StartsWith("FortniteGame/Plugins/GameFeatures/BattlePassS30_Quests/Content/Audio/VO/SoundSequences/"));
        foreach (var soundSequence in soundSequences)
        {
            var soundSequenceObject = ProviderUtils.LoadObject<UFortSoundSequence>(soundSequence.PathWithoutExtension + "." + soundSequence.NameWithoutExtension);
            Parallel.For((long)0, soundSequenceObject.SoundSequenceData.Length, i =>
            {
                var soundSequenceData = soundSequenceObject.SoundSequenceData[i];

                if (soundSequenceData.Sound.Name.StartsWith("VO_", StringComparison.OrdinalIgnoreCase) &&
                    ProviderUtils.TryGetPackageIndexExport(soundSequenceData.Sound.FirstNode, out UObject soundNodeDialoguePlayer))
                {
                    if (soundNodeDialoguePlayer.TryGetValue(out FStructFallback dialogueWaveParameter, "DialogueWaveParameter") &&
                        dialogueWaveParameter.TryGetValue(out FPackageIndex dialogueWaveIndex, "DialogueWave") &&
                        ProviderUtils.TryGetPackageIndexExport(dialogueWaveIndex, out UObject dialogueWave))
                    {
                        var voiceLines = GetSoundWave(dialogueWave);
                        var subtitles = GetSpokenText(dialogueWave);
                        
                        voiceLines.Decode(true, out var audioFormat, out var data);

                        var path = Path.Combine(ApplicationService.AudioFilesDirectory, soundSequence.NameWithoutExtension, $"{i}-{voiceLines.Name}.{audioFormat.ToLower()}");
                        Directory.CreateDirectory(path.SubstringBeforeLast("\\"));

                        File.WriteAllBytesAsync(path, data ?? throw new NullReferenceException("The selected soundwave was null"));
                        Log.Information("Exported {0} at '{1}'", voiceLines.Name, path);
                        ImageManager.MakeImage(subtitles, soundSequence.NameWithoutExtension, $"{i}-{voiceLines.Name}");
                    }
                }
            });
        }
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

        return new USoundWave();
    }

    public void DecodeBinkaToWav()
    {
        var binkaFiles = Directory.GetFiles(ApplicationService.AudioFilesDirectory, "*.binka", SearchOption.AllDirectories);
        
        var binkadecPath = Path.Combine(ApplicationService.DataDirectory, "binkadec.exe");
        if (!File.Exists(binkadecPath))
        {
            Log.Error("Binka Decoder doesn't exist in .data folder");
            return;
        }

        Parallel.ForEach(binkaFiles, binkaFile =>
        {
            var wavFilePath = Path.ChangeExtension(Path.Combine(ApplicationService.AudioFilesDirectory, binkaFile), "wav");
            var binkadecProcess = Process.Start(new ProcessStartInfo
            {
                FileName = binkadecPath,
                Arguments = $"-i \"{binkaFile}\" -o \"{wavFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            binkadecProcess?.WaitForExit(500);
            
            File.Delete(binkaFile);
            Log.Information("Successfully converted '{file1}' to .wav", binkaFile);
        });
    }
}