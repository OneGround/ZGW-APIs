using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Configuration;
using OneGround.ZGW.Besluiten.Web.Notificaties;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Besluiten.Web.Handlers;

public abstract class BesluitenBaseHandler<T> : ZGWBaseHandler
{
    protected readonly ILogger<T> _logger;
    protected readonly INotificatieService _notificatieService;
    protected readonly ApplicationConfiguration _applicationConfiguration;
    protected readonly IEntityUriService _uriService;
    protected readonly AuthorizationContext _authorizationContext;
    protected readonly IBesluitKenmerkenResolver _besluitKenmerkenResolver;

    public BesluitenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitKenmerkenResolver besluitKenmerkenResolver
    )
        : base(configuration, authorizationContextAccessor)
    {
        _logger = logger;
        _uriService = uriService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        if (_applicationConfiguration == null)
            throw new InvalidOperationException("Application section not found in appsettings.");

        _authorizationContext = authorizationContextAccessor.AuthorizationContext;
        _besluitKenmerkenResolver = besluitKenmerkenResolver;
    }

    public BesluitenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        INotificatieService notificatieService,
        IBesluitKenmerkenResolver besluitKenmerkenResolver
    )
        : this(logger, configuration, uriService, authorizationContextAccessor, besluitKenmerkenResolver)
    {
        _notificatieService = notificatieService;
    }

    private static Resource GetEntityResource(IBesluitEntity besluitEntity) =>
        besluitEntity switch
        {
            BesluitInformatieObject => Resource.besluitinformatieobject,
            _ => throw new ArgumentException(null, nameof(besluitEntity)),
        };

    public async Task SendNotificationAsync(
        Actie actie,
        Besluit besluit,
        CancellationToken cancellationToken,
        Dictionary<string, string> extraKenmerken = null
    )
    {
        var hoofdObject = _uriService.GetUri(besluit);

        var kenmerken = await _besluitKenmerkenResolver.GetKenmerkenAsync(besluit, cancellationToken);
        AddExtraKenmerken(kenmerken, extraKenmerken);

        await SendNotificationAsync(
            new Notification
            {
                HoodfObject = hoofdObject,
                Kanaal = Kanaal.besluiten.ToString(),
                Resource = Resource.besluit.ToString(),
                ResourceUrl = hoofdObject,
                Actie = actie.ToString(),
                Kenmerken = kenmerken,
                Rsin = besluit.Owner,
            },
            cancellationToken
        );
    }

    public async Task SendNotificationAsync<TBesluitEntity>(
        Actie actie,
        TBesluitEntity besluitEntity,
        CancellationToken cancellationToken,
        Dictionary<string, string> extraKenmerken = null
    )
        where TBesluitEntity : IBesluitEntity, IUrlEntity
    {
        var hoofdObject = _uriService.GetUri(besluitEntity.Besluit);
        var resourceUrl = _uriService.GetUri(besluitEntity);

        var kenmerken = await _besluitKenmerkenResolver.GetKenmerkenAsync(besluitEntity.Besluit, cancellationToken);
        AddExtraKenmerken(kenmerken, extraKenmerken);

        await SendNotificationAsync(
            new Notification
            {
                HoodfObject = hoofdObject,
                Kanaal = Kanaal.besluiten.ToString(),
                Resource = GetEntityResource(besluitEntity).ToString(),
                ResourceUrl = resourceUrl,
                Actie = actie.ToString(),
                Kenmerken = kenmerken,
                Rsin = besluitEntity.Besluit.Owner,
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

    private static void AddExtraKenmerken(Dictionary<string, string> kenmerken, Dictionary<string, string> extraKenmerken)
    {
        if (extraKenmerken != null)
        {
            foreach (var kvp in extraKenmerken)
            {
                kenmerken[kvp.Key] = kvp.Value;
            }
        }
    }
}
