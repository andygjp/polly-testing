namespace TestProject1;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class InvalidPrimaryHandler : HttpClientHandler
{
    public class Exception : System.Exception
    {
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        throw new Exception();
    }
}