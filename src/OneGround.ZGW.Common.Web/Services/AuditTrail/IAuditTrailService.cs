using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public interface IAuditTrailService : IDisposable
{
    // Meta data properties
    string Name { get; }

    // Operations to log audit trail entries
    void SetOptions(AuditTrailOptions options);

    void SetOld<TDto>(IBaseEntity entity);
    void SetNew<TDto>(IBaseEntity entity);

    Task CreatedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default);

    Task DestroyedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default);
    Task DestroyedAsync(IUrlEntity entity, string toelichting, CancellationToken cancellationToken = default);

    Task UpdatedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default);
    Task PatchedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default);
    Task PatchedAsync(IBaseEntity entity, IUrlEntity subEntity, string toelichting = null, CancellationToken cancellationToken = default);

    Task GetAsync(IBaseEntity entity, IUrlEntity subEntity, string overruleActieWeergave, CancellationToken cancellationToken = default);

    Task GetListAsync(int count, int totalCount, int page, CancellationToken cancellationToken = default);
    Task GetListAsync(int totalCount, CancellationToken cancellationToken = default);

    // Retrieval methods for audit trail entries
    Task<IEnumerable<AuditTrailRegel>> GetAuditTrailEntriesAsync(Guid hoofdobjectId, CancellationToken cancellationToken = default);
    Task<AuditTrailRegel> GetAuditTrailEntryByIdAsync(Guid hoofdobjectId, Guid audittrailId, CancellationToken cancellationToken = default);
}
