using RestSharp;

namespace Apollo.Framework;

public class FRestRequest : RestRequest
{
    private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(15 * 1000);

    public FRestRequest(string url, Method method = Method.Get) : base(url, method)
    {
        Timeout = _timeout;
    }
}