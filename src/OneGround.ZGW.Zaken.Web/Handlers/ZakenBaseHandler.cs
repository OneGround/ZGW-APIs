using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.Extensions;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers;

public abstract class ZakenBaseHandler<T> : ZGWBaseHandler
{
    protected readonly ILogger<T> _logger;
    protected readonly IEntityUriService _uriService;
    protected readonly INotificatieService _notificatieService;
    protected readonly ApplicationConfiguration _applicationConfiguration;
    protected readonly AuthorizationContext _authorizationContext;
    protected readonly IZaakKenmerkenResolver _zaakKenmerkenResolver;

    public ZakenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IEntityUriService uriService,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(configuration, authorizationContextAccessor)
    {
        _logger = logger;
        _uriService = uriService;

        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        if (_applicationConfiguration == null)
            throw new InvalidOperationException("Application section not found in appsettings.");

        _authorizationContext = authorizationContextAccessor.AuthorizationContext;
        _zaakKenmerkenResolver = zaakKenmerkenResolver;
    }

    public ZakenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : this(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _notificatieService = notificatieService;
    }

    private static Resource GetEntityResource(IZaakEntity zaakEntity) =>
        zaakEntity switch
        {
            ZaakStatus => Resource.status,
            ZaakResultaat => Resource.resultaat,
            ZaakRol => Resource.rol,
            ZaakBesluit => Resource.zaakbesluit,
            ZaakEigenschap => Resource.zaakeigenschap,
            ZaakInformatieObject => Resource.zaakinformatieobject,
            ZaakObject => Resource.zaakobject,
            KlantContact => Resource.klantcontact,
            ZaakVerzoek => Resource.zaakverzoek,
            ZaakContactmoment => Resource.zaakcontactmoment,
            _ => throw new ArgumentException(null, nameof(zaakEntity)),
        };

    public async Task SendNotificationAsync(Actie actie, Zaak zaak, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(zaak);

        await SendNotificationAsync(
            new Notification
            {
                HoodfObject = hoofdObject,
                Kanaal = Kanaal.zaken.ToString(),
                Resource = Resource.zaak.ToString(),
                ResourceUrl = hoofdObject,
                Actie = actie.ToString(),
                Kenmerken = await _zaakKenmerkenResolver.GetKenmerkenAsync(zaak, cancellationToken),
                Ignore = zaak.HasConversionKenmerk(),
                Rsin = zaak.Owner,
            },
            cancellationToken
        );
    }

    public async Task SendNotificationAsync<K>(Actie actie, K zaakEntity, CancellationToken cancellationToken)
        where K : IZaakEntity, IUrlEntity
    {
        var hoofdObject = _uriService.GetUri(zaakEntity.Zaak);
        var resourceUrl = _uriService.GetUri(zaakEntity);

        await SendNotificationAsync(
            new Notification
            {
                HoodfObject = hoofdObject,
                Kanaal = Kanaal.zaken.ToString(),
                Resource = GetEntityResource(zaakEntity).ToString(),
                ResourceUrl = resourceUrl,
                Actie = actie.ToString(),
                Kenmerken = await _zaakKenmerkenResolver.GetKenmerkenAsync(zaakEntity.Zaak, cancellationToken),
                Ignore = zaakEntity.HasConversionKenmerk(),
                Rsin = zaakEntity.Zaak.Owner,
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
}
