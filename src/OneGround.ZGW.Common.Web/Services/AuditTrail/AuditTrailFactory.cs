using System;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public class AuditTrailFactory : IAuditTrailFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AuditTrailFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IAuditTrailService Create(bool legacy = true)
    {
        return Create(new AuditTrailOptions(), legacy);
    }

    public IAuditTrailService Create(AuditTrailOptions options, bool legacy = true)
    {
        IAuditTrailService requestService = legacy
            ? _serviceProvider.GetRequiredService<AuditTrailService>()
            : _serviceProvider.GetRequiredService<DeltaBasedAuditTrail>();

        requestService.SetOptions(options ?? new AuditTrailOptions());

        return requestService;
    }
}
