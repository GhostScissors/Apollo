using Apollo.Service;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace Apollo.Utils;

public static class ProviderUtils
{
    public static async Task<T> LoadObjectAsync<T>(string packagePath) where T : UObject
    {
        return await ApplicationService.CUE4Parse.Provider.LoadObjectAsync<T>(packagePath).ConfigureAwait(false);
    }
    
    public static T LoadObject<T>(string packagePath) where T : UObject
    {
        return LoadObjectAsync<T>(packagePath).GetAwaiter().GetResult();
    }

    public static bool TryGetPackageIndexExport<T>(FPackageIndex? packageIndex, out T export) where T : UObject
    {
        return packageIndex!.TryLoad(out export);
    }
}