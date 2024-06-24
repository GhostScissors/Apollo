using System.Diagnostics;
using Apollo.Enums;
using Apollo.Managers;
using Apollo.Service;
using Apollo.Utils;
using Serilog;
using Spectre.Console;

namespace Apollo;

public class Program
{
    public static async Task Main(string[] args)
    {
        var updateModeString = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Choose the [45]Update mode[/]")
            .PageSize(10)
            .HighlightStyle("45")
            .MoreChoicesText("[grey](Move up and down to see more options)[/]")
            .AddChoices([
                "Update Mode",
                "Get New"
            ]));

        var updateMode = updateModeString switch
        {
            "Update Mode" => EUpdateMode.UpdateMode,
            _ => EUpdateMode.UpdateMode
        };

        var stopwatch = Stopwatch.StartNew();
        
        ApplicationService.Initialize();
        await ApplicationService.DownloadDependencies().ConfigureAwait(false);
        await ApplicationService.ApiVM.EpicApi.VerifyAuth().ConfigureAwait(false);
        await ApplicationService.CUE4ParseVM.Initialize(updateMode).ConfigureAwait(false);
        await ApplicationService.CUE4ParseVM.LoadMappings().ConfigureAwait(false);
        await ApplicationService.CUE4ParseVM.LoadNewFiles().ConfigureAwait(false);
        ApplicationService.SoundsVM.ExportBinkaAudioFiles();
        ApplicationService.SoundsVM.DecodeBinkaToWav();
        VideoManager.MakeFinalVideo();
        ApplicationService.Deinitialize();

        stopwatch.Stop();
        
        Log.Information("All operations completed in {time} minutes. Press any key to exit", stopwatch.Elapsed.TotalMinutes);
        Console.ReadKey();
    }
}