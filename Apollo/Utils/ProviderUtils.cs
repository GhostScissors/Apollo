using Apollo.Service;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace Apollo.Utils;

public class ProviderUtils
{
    public static T LoadObject<T>(string packagePath) where T : UObject
    {
        return ApplicationService.CUE4ParseVM.Provider.LoadObject<T>(packagePath);
    }

    public static bool TryGetPackageIndexExport<T>(FPackageIndex? packageIndex, out T export) where T : UObject
    {
        return packageIndex!.TryLoad(out export);
    }
}