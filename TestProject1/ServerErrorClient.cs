namespace TestProject1;

using System.Net.Http;
using System.Threading.Tasks;

public class ServerErrorClient : Client
{
    private readonly HttpClient httpClient;

    public ServerErrorClient(HttpClient httpClient) => this.httpClient = httpClient;

    public Task<HttpResponseMessage> Call() => httpClient.GetAsync("http://localhost:5282/transientError");
}