using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Catalogi.Web.Notificaties;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

public abstract class CatalogiBaseHandler<T> : ZGWBaseHandler
{
    protected readonly ILogger<T> _logger;
    protected readonly IEntityUriService _uriService;
    protected readonly INotificatieService _notificatieService;
    protected readonly ApplicationConfiguration _applicationConfiguration;

    public CatalogiBaseHandler(
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
    }

    public CatalogiBaseHandler(
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

    private Dictionary<string, string> GetKenmerken(ICatalogusEntity entity)
    {
        return new Dictionary<string, string> { { "catalogus", _uriService.GetUri(entity.Catalogus) }, { "domein", entity.Catalogus.Domein } };
    }

    protected async Task SendNotificationAsync(Actie actie, ICatalogusEntity entity, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(entity);
        var notification = new Notification
        {
            HoodfObject = hoofdObject,
            Kanaal = GetEntityKanaal(entity).ToString(),
            Resource = GetEntityResource(entity).ToString(),
            ResourceUrl = hoofdObject,
            Kenmerken = GetKenmerken(entity),
            Rsin = entity.Catalogus.Owner,
            Actie = actie.ToString(),
        };

        if (!_applicationConfiguration.DontSendNotificaties)
        {
            await _notificatieService.NotifyAsync(notification, cancellationToken);
        }
        else
        {
            _logger.LogDebug("Warning: Notifications are disabled. Notification {@Notification} will not be sent.", notification);
        }
    }

    private static Kanaal GetEntityKanaal(ICatalogusEntity entity) =>
        entity switch
        {
            ZaakType => Kanaal.zaaktypen,
            BesluitType => Kanaal.besluittypen,
            InformatieObjectType => Kanaal.informatieobjecttypen,
            _ => throw new ArgumentException($"Cannot resolve entity {nameof(Kanaal)} value.", nameof(entity)),
        };

    private static Resource GetEntityResource(ICatalogusEntity entity) =>
        entity switch
        {
            ZaakType => Resource.zaaktype,
            BesluitType => Resource.besluittype,
            InformatieObjectType => Resource.informatieobjecttype,
            _ => throw new ArgumentException($"Cannot resolve entity {nameof(Resource)} value.", nameof(entity)),
        };
}
