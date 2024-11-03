using System.Diagnostics;
using Apollo.Enums;
using Apollo.Export;
using Apollo.Service;
using Serilog;
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
        var updateMode = EUpdateMode.GetNewFiles;
#endif
        var pakNumber = "30";
        
        if (updateMode == EUpdateMode.PakFiles)
            pakNumber = AnsiConsole.Prompt(new TextPrompt<string>("Please input the pak number you want to load:")
                .PromptStyle("green")
                .Validate(f => f.Length == 4 ? ValidationResult.Success() : ValidationResult.Error("[red]Please enter a valid 4-digit pak number.[/]")));

        var bOverrideMappings = AnsiConsole.Prompt(new TextPrompt<bool>("Do you want to override mappings path?")
            .PromptStyle("green")
            .DefaultValue(false)
            .AddChoice(true)
            .AddChoice(false));
        
        var stopwatch = Stopwatch.StartNew();
        
        await ApplicationService.Initialize().ConfigureAwait(false);
        await ApplicationService.CUE4ParseVM.Initialize(updateMode, pakNumber, bOverrideMappings).ConfigureAwait(false);
        await Exporter.VoiceLines.Export();
        await DiscordService.InitializeAsync().ConfigureAwait(false);

        stopwatch.Stop();
        
        Log.Information("All operations completed in {time} seconds. Press any key to exit", stopwatch.Elapsed.TotalSeconds);
        Console.ReadKey();
    }
}