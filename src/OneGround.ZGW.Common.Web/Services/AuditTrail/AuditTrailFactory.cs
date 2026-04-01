using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public class AuditTrailFactory : IAuditTrailFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AuditTrailFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IAuditTrailService Create(AuditTrailOptions options, bool legacy = true)
    {
        //var requestService = _serviceProvider
        //    .GetServices<IAuditTrailService>()
        //    .SingleOrDefault(s => legacy && s.Name == "Legacy" || !legacy && s.Name == "Deltas");

        var requestService = _serviceProvider.GetServices<IAuditTrailService>().SingleOrDefault(s => legacy == s.Legacy);

        if (requestService == null)
        {
            throw new InvalidOperationException($"No audit trailservice found for useDeltas={legacy}");
        }

        requestService.SetOptions(options ?? new AuditTrailOptions());

        return requestService;
    }
}
