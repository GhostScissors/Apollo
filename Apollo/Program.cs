using Apollo.Service;
using Apollo.Utils;
using Serilog;

namespace Apollo;

public class Program
{
    public static async Task Main(string[] args)
    {
        ApplicationService.Initialize();
        await ApplicationService.DownloadDependencies().ConfigureAwait(false);
        await ApplicationService.ApiVM.EpicApi.VerifyAuth().ConfigureAwait(false);
        await ApplicationService.CUE4ParseVM.Initialize().ConfigureAwait(false);
        await ApplicationService.CUE4ParseVM.LoadMappings().ConfigureAwait(false);
        await ApplicationService.CUE4ParseVM.LoadNewFiles().ConfigureAwait(false);
        ApplicationService.SoundsVM.ExportBinkaAudioFiles();
        ApplicationService.SoundsVM.TryDecode();
        VideoUtils.MakeFinalVideo();
        ApplicationService.Deinitialize();

        Log.Information("All operations done. Press any key to exit");
        Console.ReadKey();
    }
}