﻿using System.Diagnostics;
using Apollo.Enums;
using Apollo.Managers;
using Apollo.Service;
using Apollo.Utils;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Spectre.Console;

namespace Apollo;

public class Program
{
    public static async Task Main(string[] args)
    {
#if !DEBUG
        var updateMode = AnsiConsole.Prompt(new SelectionPrompt<EUpdateMode>()
            .Title("Choose the [45]Update mode[/]")
            .PageSize(10)
            .HighlightStyle("45")
            .MoreChoicesText("[grey](Move up and down to see more options)[/]")
            .AddChoices(Enum.GetValues<EUpdateMode>()));
#else
        var updateMode = EUpdateMode.GetNew;
#endif

        var stopwatch = Stopwatch.StartNew();
        
        await ApplicationService.Initialize().ConfigureAwait(false);
        await ApplicationService.CUE4ParseVM.Initialize(updateMode).ConfigureAwait(false);
        ApplicationService.SoundsVM.ExportBinkaAudioFiles();
        ApplicationService.SoundsVM.DecodeBinkaToWav();
        VideoManager.MakeFinalVideo();

        stopwatch.Stop();
        
        Log.Information("All operations completed in {time} seconds. Press any key to exit", stopwatch.Elapsed.TotalSeconds);
        Console.ReadKey();
    }
}