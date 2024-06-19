using Apollo.Framework;
using Apollo.ViewModels.API.Models;
using RestSharp;
using Serilog;

namespace Apollo.ViewModels.API;

public class FortniteCentralApiEndpoint : AbstractApiProvider
{
    private const string MAPPINGS_URL = "https://fortnitecentral.genxgames.gg/api/v1/mappings";

    public FortniteCentralApiEndpoint(RestClient client)  : base(client) { }

    public async Task<MappingsResponse[]?> GetMappingsAsync()
    {
        var request = new FRestRequest(MAPPINGS_URL);
        var response = await _client.ExecuteAsync<MappingsResponse[]>(request);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data;
    }
}