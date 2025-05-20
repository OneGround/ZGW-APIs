using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Secrets;

namespace OneGround.ZGW.Common.Services;

public interface ICachedZGWSecrets
{
    public Task<ServiceSecret> GetServiceSecretAsync(string rsin, string service, CancellationToken cancellationToken);
}
