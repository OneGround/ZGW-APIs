using System;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Contracts;

public interface INummerGenerator
{
    void SetTemplateKeyValue(string key, string value);
    Task<string> GenerateAsync(string rsin, string entity, CancellationToken cancellationToken = default);
    Task<string> GenerateAsync(string rsin, string entity, Func<string, bool> IsUnique, CancellationToken cancellationToken = default);

    /// <summary>
    /// The format (template) the numbers of the entity are generated with for the organisation: the one stored with its first number,
    /// otherwise the configured one. Null if there is none.
    /// </summary>
    Task<string> GetFormatAsync(string rsin, string entity, CancellationToken cancellationToken = default);
}
