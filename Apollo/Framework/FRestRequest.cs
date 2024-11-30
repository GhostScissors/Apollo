using RestSharp;

namespace Apollo.Framework;

public class FRestRequest : RestRequest
{
    public FRestRequest(string url, Method method = Method.Get) : base(url, method)
    {
        Timeout = TimeSpan.FromSeconds(15);
    }
}