using RestSharp;

namespace Apollo.ViewModels.API;

public class AbstractApiProvider
{
    protected readonly RestClient _client;

    public AbstractApiProvider(RestClient client)
    {
        _client = client;
    }
}