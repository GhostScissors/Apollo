using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Apollo.ViewModels.API.Models;

public class AuthResponse
{
    [J("access_token")] public string AccessToken { get; set; }
    [J("expires_at")] public DateTime ExpiresAt { get; set; }
}