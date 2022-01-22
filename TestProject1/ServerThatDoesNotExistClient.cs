namespace TestProject1;

using System.Net.Http;
using System.Threading.Tasks;

public class ServerThatDoesNotExistClient : Client
{
    private readonly HttpClient httpClient;

    public ServerThatDoesNotExistClient(HttpClient httpClient) => this.httpClient = httpClient;

    public Task<HttpResponseMessage> Call() => httpClient.GetAsync("http://server-that-does-not-exist:52487");
}