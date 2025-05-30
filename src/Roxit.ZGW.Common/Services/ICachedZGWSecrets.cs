using System.Threading;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Secrets;

namespace Roxit.ZGW.Common.Services;

public interface ICachedZGWSecrets
{
    public Task<ServiceSecret> GetServiceSecretAsync(string rsin, string service, CancellationToken cancellationToken);
}
