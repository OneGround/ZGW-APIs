using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.Extensions;
using Roxit.ZGW.Zaken.DataModel.ZaakObject;
using Roxit.ZGW.Zaken.DataModel.ZaakRol;
using Roxit.ZGW.Zaken.Web.Configuration;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers;

public abstract class ZakenBaseHandler<T> : ZGWBaseHandler
{
    protected readonly ILogger<T> _logger;
    protected readonly IEntityUriService _uriService;
    protected readonly INotificatieService _notificatieService;
    protected readonly ApplicationConfiguration _applicationConfiguration;
    protected readonly AuthorizationContext _authorizationContext;

    public ZakenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IEntityUriService uriService
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

    public ZakenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IEntityUriService uriService,
        INotificatieService notificatieService
    )
        : this(logger, configuration, authorizationContextAccessor, uriService)
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

    private static Dictionary<string, string> GetKenmerken(Zaak zaak)
    {
        return new Dictionary<string, string>
        {
            { "bronorganisatie", zaak.Bronorganisatie },
            { "zaaktype", zaak.Zaaktype },
            { "vertrouwelijkheidaanduiding", zaak.VertrouwelijkheidAanduiding.ToString() },
        };
    }

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
                Kenmerken = GetKenmerken(zaak),
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
                Kenmerken = GetKenmerken(zaakEntity.Zaak),
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
