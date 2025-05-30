using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Batching;
using Roxit.ZGW.Common.Constants;

namespace Roxit.ZGW.Common.ServiceAgent;

public class BatchIdHandler : DelegatingHandler
{
    private readonly IBatchIdAccessor _batchIdAccessor;

    public BatchIdHandler(IBatchIdAccessor batchIdAccessor)
    {
        _batchIdAccessor = batchIdAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(Headers.BatchId) && !string.IsNullOrEmpty(_batchIdAccessor.Id))
        {
            request.Headers.Add(Headers.BatchId, _batchIdAccessor.Id);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
