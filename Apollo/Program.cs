using Apollo.Enums;
using Apollo.Export;
using Apollo.Service;
using Serilog;

namespace Apollo;

public static class Program
{
    private const int MaxRetries = 5;
    private static int _attempts = 1;
    
    public static async Task Main(string[] args)
    {
        await ApplicationService.InitializeAsync().ConfigureAwait(false);
        await DiscordService.InitializeAsync().ConfigureAwait(false);
        
        while (_attempts <= MaxRetries)
        {
            try
            {
                var updateMode = _attempts == 1 ? EUpdateMode.WaitForUpdate : EUpdateMode.GetNewFiles;
                
                Log.Information("UpdateMode: {0} Attempts: '{1}'", updateMode, _attempts);
                
                await ApplicationService.CUE4Parse.InitializeAsync(updateMode).ConfigureAwait(false);
                await Exporter.VoiceLines.ExportAsync().ConfigureAwait(false);
                await DiscordService.SendVideoAsync();
                
                Log.Information("All operations completed in seconds. Press any key to exit");
            }
            catch (Exception e)
            {
                _attempts++;
                Console.WriteLine(e);
                
                if (_attempts > 5)
                    ResetAttempts();
                
                Thread.Sleep(2500);
            }
        }
    }

    private static void ResetAttempts() => _attempts = 1;
}