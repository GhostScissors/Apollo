using Apollo.ViewModels.API;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace Apollo.ViewModels;

public class ApiEndpointViewModel
{
    public EpicApiEndpoint EpicApi;

    private readonly RestClient _client = new(new RestClientOptions
    {
        UserAgent = "Apollo",
        Timeout = TimeSpan.FromSeconds(15),
    }, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());

    public ApiEndpointViewModel()
    {
        EpicApi = new(_client);
    }
}