using System;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Contracts;

public interface INummerGenerator
{
    void SetTemplateKeyValue(string key, string value);
    Task<string> GenerateAsync(string rsin, string entity, CancellationToken cancellationToken = default);
    Task<string> GenerateAsync(string rsin, string entity, Func<string, bool> IsUnique, CancellationToken cancellationToken = default);
}
