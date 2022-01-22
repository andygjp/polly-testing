namespace TestProject1;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class PrimaryHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // do some work
        return await base.SendAsync(request, cancellationToken);
    }
}