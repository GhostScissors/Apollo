using Apollo.Framework;
using Apollo.ViewModels.API.Models;
using RestSharp;
using Serilog;

namespace Apollo.ViewModels.API;

public class FModelApiEndpoint : AbstractApiProvider
{
    private const string BACKUPS_URL = "https://api.fmodel.app/v1/backups/FortniteGame";

    public FModelApiEndpoint(RestClient client) : base(client) { }

    public async Task<BackupResponse[]?> GetBackupsAsync()
    {
        var request = new FRestRequest(BACKUPS_URL);
        var response = await _client.ExecuteAsync<BackupResponse[]>(request).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data;
    }
}