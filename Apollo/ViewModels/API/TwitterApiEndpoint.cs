using Apollo.Framework;
using RestSharp;
using Serilog;

namespace Apollo.ViewModels.API;

public class TwitterApiEndpoint : AbstractApiProvider
{
    static string apiKey = "0rB7satCUooLyZGLxiuyCCNuL";
    static string apiSecret = "XEFH7bNRXpfvR9qGQLqH26DEkSk3FTFWOsTLBftbOLBzPoBbZa";
    static string accessKey = "1051137711757815808-xlQhQm9qhkMScQ86ercoIgUTr3uMNE";
    static string accessSecret = "Qnpo4hXy9JSjnjgYW7S2jA7MEj7KAoeUhvW7lpfRWHoPs";
    static string bearerToken = "AAAAAAAAAAAAAAAAAAAAAJpDnwEAAAAAXC2a4oZXuk01JhdP7yH6m%2BSZ%2Bko%3Djn1K2PDBtnlHAJfYf5bfoN2iTKxQyPtkzVSTcOlgQP1eZBhKAz";
    
    public TwitterApiEndpoint(RestClient client) : base(client) { }

    public async Task Upload()
    {
        
    }
    
}