using System;
using Microsoft.Extensions.DependencyInjection;

namespace Roxit.ZGW.Common.Web.Services.AuditTrail;

public class AuditTrailFactory : IAuditTrailFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AuditTrailFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IAuditTrailService Create(AuditTrailOptions options)
    {
        var result = _serviceProvider.GetService<IAuditTrailService>();

        result.SetOptions(options);

        return result;
    }
}
