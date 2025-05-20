using System;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public interface IAuditTrailService : IDisposable
{
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

    Task GetListAsync(int count, int totalCount, int page, CancellationToken cancellationToken);
    Task GetListAsync(int totalCount, CancellationToken cancellationToken);
}
