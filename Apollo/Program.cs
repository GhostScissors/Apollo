using Apollo.Service;

ApplicationService.Initialize();
await ApplicationService.ApiVM.EpicApi.VerifyAuth();
ApplicationService.Deinitialize();