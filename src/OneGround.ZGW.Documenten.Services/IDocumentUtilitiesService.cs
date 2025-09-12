using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Documenten.Services;

public interface IDocumentUtilitiesService
{
    string ProviderPrefix { get; }

    Task<IEnumerable<string>> EnumFolderNamesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> EnumDocumentsAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> DeleteFolderAsync(string name, CancellationToken cancellationToken = default);
}
