using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Apollo.ViewModels.API.Models;

public class AesResponse
{
    [J("mainKey")] public string MainKey { get; set; }
    [J("dynamicKeys")] public List<DynamicKeys> DynamicKeys { get; set; }
}

public class DynamicKeys
{
    [J("key")] public string Key { get; set; }
    [J("guid")] public string Guid { get; set; }
}