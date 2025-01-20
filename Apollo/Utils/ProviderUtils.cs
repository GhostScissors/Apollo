using Apollo.Service;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace Apollo.Utils;

public static class ProviderUtils
{
    public static async Task<UObject> LoadObject(string packagePath)
    {
        return await ApplicationService.CUE4Parse.Provider.LoadObjectAsync(packagePath).ConfigureAwait(false);
    }
    
    public static async Task<T> LoadObject<T>(string packagePath) where T : UObject
    {
        return await ApplicationService.CUE4Parse.Provider.LoadObjectAsync<T>(packagePath).ConfigureAwait(false);
    }
    
    public static async Task<IEnumerable<UObject>> LoadAllObjects(string packagePath)
    {
        return await ApplicationService.CUE4Parse.Provider.LoadAllObjectsAsync(packagePath).ConfigureAwait(false);
    }

    public static bool TryGetPackageIndexExport<T>(FPackageIndex? packageIndex, out T export) where T : UObject
    {
        return packageIndex!.TryLoad(out export);
    }
}