using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;

namespace Apollo.Service;

public static class DiscordService
{
    private const string Token = "MTI3NzI5Njc5MTU2OTgyOTk1OQ.Gf2hml.u9Yc8y-7FDPiXveefJl63Y9Y4gXnm38VAE2yiM"; // Asval please don't do something shady with this
    private const ulong ChannelId = 1277295260111994912;

    private static readonly DiscordSocketClient Client;

    static DiscordService()
    {
        Client = new DiscordSocketClient();
    }

    public static async Task InitializeAsync()
    {
        Client.Log += LogAsync;
        
        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();

        await SendVideo();
    }

    private static async Task SendVideo()
    {
        var channel = await Client.GetChannelAsync(ChannelId) as IMessageChannel;
        var videoPath = Path.Combine(ApplicationService.ExportDirectory, "output.mp4");

        if (!File.Exists(videoPath) || channel == null) return;

        await channel.SendFileAsync(videoPath, "@everyone");
    }
    
    private static async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        
        Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        await Task.CompletedTask;
    }
}