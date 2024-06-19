using Apollo.Service;
using Apollo.ViewModels;
using Newtonsoft.Json;

ApplicationService.Initialize();
await ApplicationService.ApiVM.EpicApi.VerifyAuth().ConfigureAwait(false);
await ApplicationService.CUE4ParseVM.Initialize().ConfigureAwait(false);
await ApplicationService.CUE4ParseVM.LoadMappings().ConfigureAwait(false);
await ApplicationService.CUE4ParseVM.LoadNewFiles().ConfigureAwait(false);
ApplicationService.Deinitialize();