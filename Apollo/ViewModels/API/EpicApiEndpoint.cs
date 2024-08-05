using Apollo.Framework;
using Apollo.ViewModels.API.Models;
using EpicManifestParser.Api;
using RestSharp;
using Serilog;

namespace Apollo.ViewModels.API;

public class EpicApiEndpoint : AbstractApiProvider
{
    private const string AUTH_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string MANFEST_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";
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
    

    public async Task<ManifestInfo?> GetManifestAsync()
    {
        var auth = await CreateAuthAsync().ConfigureAwait(false);
        
        var request = new FRestRequest(MANFEST_URL);
        request.AddHeader("Authorization", $"bearer {auth?.AccessToken}");
        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return ManifestInfo.Deserialize(response.RawBytes);
    }
}