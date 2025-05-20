using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Caching;

namespace OneGround.ZGW.Common.ServiceAgent.Extensions;

public static class CachingExtensions
{
    public static async Task<CachedResponse> ToCachedResponse(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var headers = response.Headers.Where(h => h.Value != null && h.Value.Any()).ToDictionary(h => h.Key, h => h.Value);

        return new CachedResponse { Content = content, Headers = headers };
    }

    public static HttpResponseMessage ToHttpResponseMessage(this CachedResponse cachedResponse)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(cachedResponse.Content) };

        foreach (var header in cachedResponse.Headers)
        {
            response.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return response;
    }
}
