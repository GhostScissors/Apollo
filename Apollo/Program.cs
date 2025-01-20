using Apollo.Enums;
using Apollo.Export;
using Apollo.Service;
using Serilog;

namespace Apollo;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await ApplicationService.InitializeAsync().ConfigureAwait(false);
        await DiscordService.InitializeAsync().ConfigureAwait(false);
        
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
        
        Log.Information("UpdateMode: {0}'", updateMode);
            
        await ApplicationService.CUE4Parse.InitializeAsync(updateMode).ConfigureAwait(false);
        await Exporter.VoiceLines.ExportAsync().ConfigureAwait(false);
        await DiscordService.SendVideoAsync();
            
        Log.Information("All operations completed in seconds. Press any key to exit");
    }
}