using Apollo.Settings;
using Apollo.ViewModels;
using Apollo.ViewModels.API;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Apollo.Service;

public sealed class ApplicationService
{
    public static DirectoryInfo OutputDirectory = new(Path.Combine(Environment.CurrentDirectory, "Output"));
    public static DirectoryInfo DataDirectory = new(Path.Combine(OutputDirectory.FullName, ".data"));
    
    public static ApiEndpointViewModel ApiVM = new();
    
    public static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .CreateLogger();

        AppSettings.Load();

        OutputDirectory.Create();
        DataDirectory.Create();
    }

    public static void Deinitialize()
    {
        AppSettings.Save();
    }
}