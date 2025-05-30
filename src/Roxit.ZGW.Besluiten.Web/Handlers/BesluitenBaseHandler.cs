using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Besluiten.Web.Configuration;
using Roxit.ZGW.Besluiten.Web.Notificaties;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Besluiten.Web.Handlers;

public abstract class BesluitenBaseHandler<T> : ZGWBaseHandler
{
    protected readonly ILogger<T> _logger;
    protected readonly INotificatieService _notificatieService;
    protected readonly ApplicationConfiguration _applicationConfiguration;
    protected readonly IEntityUriService _uriService;
    protected readonly AuthorizationContext _authorizationContext;

    public BesluitenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(configuration, authorizationContextAccessor)
    {
        _logger = logger;
        _uriService = uriService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        if (_applicationConfiguration == null)
            throw new InvalidOperationException("Application section not found in appsettings.");

        _authorizationContext = authorizationContextAccessor.AuthorizationContext;
    }

    public BesluitenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        INotificatieService notificatieService
    )
        : this(logger, configuration, uriService, authorizationContextAccessor)
    {
        _notificatieService = notificatieService;
    }

    public async Task SendNotificationAsync(Actie actie, Besluit besluit, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(besluit);

        await SendNotificationAsync(
            new Notification
            {
                HoodfObject = hoofdObject,
                Kanaal = Kanaal.besluiten.ToString(),
                Resource = Resource.besluit.ToString(),
                ResourceUrl = hoofdObject,
                Actie = actie.ToString(),
                Kenmerken = GetKenmerken(besluit),
                Rsin = besluit.Owner,
            },
            cancellationToken
        );
    }

    public async Task SendNotificationAsync(Actie actie, BesluitInformatieObject besluitInformatieObject, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(besluitInformatieObject.Besluit);
        var resourceUrl = _uriService.GetUri(besluitInformatieObject);

        await SendNotificationAsync(
            new Notification
            {
                HoodfObject = hoofdObject,
                Kanaal = Kanaal.besluiten.ToString(),
                Resource = Resource.besluitinformatieobject.ToString(),
                ResourceUrl = resourceUrl,
                Actie = actie.ToString(),
                Kenmerken = GetKenmerken(besluitInformatieObject.Besluit),
                Rsin = besluitInformatieObject.Besluit.Owner,
            },
            cancellationToken
        );
    }

    private async Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
    {
        if (!_applicationConfiguration.DontSendNotificaties)
        {
            await _notificatieService.NotifyAsync(notification, cancellationToken);
        }
        else
        {
            _logger.LogDebug("Warning: Notifications are disabled. Notification {@Notification} will not be sent.", notification);
        }
    }

    private static Dictionary<string, string> GetKenmerken(Besluit besluit)
    {
        return new Dictionary<string, string>
        {
            { "verantwoordelijke_organisatie", besluit.VerantwoordelijkeOrganisatie },
            { "besluittype", besluit.BesluitType },
        };
    }
}
