using Apollo.Service;
using Apollo.Utils;
using Apollo.ViewModels;
using Newtonsoft.Json;
using Serilog;

ApplicationService.Initialize();
await ApplicationService.InitVgmStream();
await ApplicationService.ApiVM.EpicApi.VerifyAuth().ConfigureAwait(false);
await ApplicationService.CUE4ParseVM.Initialize().ConfigureAwait(false);
await ApplicationService.CUE4ParseVM.LoadMappings().ConfigureAwait(false);
await ApplicationService.CUE4ParseVM.LoadNewFiles().ConfigureAwait(false);
ApplicationService.SoundsVM.ExportBinkaAudioFiles();
ApplicationService.SoundsVM.ConvertBinkaToWav();
VideoUtils.MakeFinalVideo();
ApplicationService.Deinitialize();

Log.Information("All operations done. Press any key to exit");
Console.ReadKey();