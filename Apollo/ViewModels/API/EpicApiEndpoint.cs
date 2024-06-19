using Apollo.Framework;
using Apollo.Settings;
using Apollo.ViewModels.API.Models;
using RestSharp;
using Serilog;

namespace Apollo.ViewModels.API;

public class EpicApiEndpoint : AbstractApiProvider
{
    private const string AUTH_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string VERIFY_URL = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify";
    private const string BASIC_TOKEN = "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=";
    
    public EpicApiEndpoint(RestClient client) : base(client) { }
    
    private async Task<AuthResponse?> CreateAuthAsync()
    {
        var request = new FRestRequest(AUTH_URL, Method.Post);
        request.AddHeader("Authorization", BASIC_TOKEN);
        request.AddParameter("grant_type", "client_credentials");
        var response = await _client.ExecuteAsync<AuthResponse>(request).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data;
    }

    private async Task<bool> IsExpired()
    {
        var request = new FRestRequest(VERIFY_URL);
        request.AddHeader("Authorization", $"bearer {AppSettings.Current.LastAuthResponse.AccessToken}");
        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        return !response.IsSuccessful;
    }

    public async Task VerifyAuth()
    {
        if (await IsExpired().ConfigureAwait(false))
        {
            var auth = await CreateAuthAsync().ConfigureAwait(false);
            if (auth != null)
            {
                AppSettings.Current.LastAuthResponse = auth;
            }
        }
    }
}