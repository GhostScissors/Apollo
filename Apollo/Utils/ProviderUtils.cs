using Apollo.Service;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace Apollo.Utils;

public class ProviderUtils
{
    public static UObject LoadAllObjects(string packagePath)
    {
        return ApplicationService.CUE4ParseVM.Provider.LoadAllObjects(packagePath).First();
    }
    
    public static bool TryLoadObject<T>(string fullPath, out T export) where T : UObject
    {
        return ApplicationService.CUE4ParseVM.Provider.TryLoadObject(fullPath, out export);
    }

    public static bool TryGetPackageIndexExport<T>(FPackageIndex? packageIndex, out T export) where T : UObject
    {
        return packageIndex.TryLoad(out export);
    }
}