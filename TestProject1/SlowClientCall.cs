namespace TestProject1;

using System.Net.Http;
using System.Threading.Tasks;

public class SlowClientCall : Client
{
    private readonly HttpClient httpClient;

    public SlowClientCall(HttpClient httpClient) => this.httpClient = httpClient;

    public Task<HttpResponseMessage> Call() => httpClient.GetAsync("http://localhost:5282/timeout");
}