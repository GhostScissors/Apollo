using Apollo.Service;
using Apollo.Utils;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Serilog;

namespace Apollo.ViewModels;

public class SoundsViewModel
{
    public void IdkWhatToNameThis()
    {
        var soundSequences = ApplicationService.CUE4ParseVM.NewEntries.Where(x => x.Path.StartsWith("FortniteGame/Plugins/GameFeatures/BattlePassS30_Quests/Content/Audio/VO/SoundSequences/")).ToList().OrderBy(entry => entry.Name);
        foreach (var soundSequence in soundSequences)
        {
            var dialogueUObject = ProviderUtils.LoadAllObjects(soundSequence.Path);
            if (dialogueUObject.TryGetValue(out FStructFallback[] soundSequencesData, "SoundSequenceData"))
            {
                foreach (var soundSequenceData in soundSequencesData)
                {
                    var (subtitles, VO) = LoadDialogueWave(soundSequenceData);
                    
                    if (!string.IsNullOrWhiteSpace(subtitles))
                        Log.Information(subtitles);
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

        return (null, null);
    }

    private string GetSpokenText(UObject dialogueWave)
    {
        return dialogueWave.TryGetValue(out string spokenText, "SpokenText") ? spokenText : "No subtitles found";
    }

    private USoundWave GetSoundWave(UObject dialogueWave)
    {
        if (dialogueWave.TryGetValue(out FStructFallback[] contextMappings, "ContextMappings"))
        {
        }

        return new USoundWave();
    }
}