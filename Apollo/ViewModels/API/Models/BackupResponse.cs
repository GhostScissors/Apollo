using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Apollo.ViewModels.API.Models;

public class BackupResponse
{
    [J("fileName")] public string FileName { get; set; }
    [J("downloadUrl")] public string DownloadUrl { get; set; }
}