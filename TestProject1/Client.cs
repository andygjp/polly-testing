namespace TestProject1;

using System.Net.Http;
using System.Threading.Tasks;

public interface Client
{
    Task<HttpResponseMessage> Call();
}