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
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Configuration;
using OneGround.ZGW.Documenten.Web.Notificaties;

namespace OneGround.ZGW.Documenten.Web.Handlers;

public abstract class DocumentenBaseHandler<T> : ZGWBaseHandler
{
    protected readonly ILogger<T> _logger;
    protected readonly INotificatieService _notificatieService;
    protected readonly IEntityUriService _uriService;
    protected readonly ApplicationConfiguration _applicationConfiguration;
    protected readonly AuthorizationContext _authorizationContext;
    protected readonly IDocumentKenmerkenResolver _documentKenmerkenResolver;

    public DocumentenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(configuration, authorizationContextAccessor)
    {
        _logger = logger;
        _uriService = uriService;

        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        if (_applicationConfiguration == null)
            throw new InvalidOperationException("Application section not found in appsettings.");

        _authorizationContext = authorizationContextAccessor.AuthorizationContext;
        _documentKenmerkenResolver = documentKenmerkenResolver;
    }

    public DocumentenBaseHandler(
        ILogger<T> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        INotificatieService notificatieService,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : this(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _notificatieService = notificatieService;
    }

    private static Resource GetEntityResource(IInformatieObjectEntity informatieObjectEntity) =>
        informatieObjectEntity switch
        {
            EnkelvoudigInformatieObject => Resource.enkelvoudiginformatieobject,
            GebruiksRecht => Resource.gebruiksrechten,
            Verzending => Resource.verzending,
            _ => throw new ArgumentException(null, nameof(informatieObjectEntity)),
        };

    public async Task SendNotificationAsync(Actie actie, EnkelvoudigInformatieObject informatieObject, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(informatieObject);

        await SendNotificationAsync(
            new Notification
            {
                HoodfObject = hoofdObject,
                Kanaal = Kanaal.documenten.ToString(),
                Resource = Resource.enkelvoudiginformatieobject.ToString(),
                ResourceUrl = hoofdObject,
                Actie = actie.ToString(),
                Kenmerken = await _documentKenmerkenResolver.GetKenmerkenAsync(informatieObject, cancellationToken),
                Rsin = informatieObject.Owner,
            },
            cancellationToken
        );
    }

    public async Task SendNotificationAsync<TInformatieObjectEntity>(
        Actie actie,
        TInformatieObjectEntity informatieObjectEntity,
        CancellationToken cancellationToken
    )
        where TInformatieObjectEntity : IInformatieObjectEntity, IUrlEntity
    {
        var hoofdObject = _uriService.GetUri(informatieObjectEntity.InformatieObject);
        var resourceUrl = _uriService.GetUri(informatieObjectEntity);

        await SendNotificationAsync(
            new Notification
            {
                HoodfObject = hoofdObject,
                Kanaal = Kanaal.documenten.ToString(),
                Resource = GetEntityResource(informatieObjectEntity).ToString(),
                ResourceUrl = resourceUrl,
                Actie = actie.ToString(),
                Kenmerken = await _documentKenmerkenResolver.GetKenmerkenAsync(informatieObjectEntity.InformatieObject, cancellationToken),
                Rsin = informatieObjectEntity.InformatieObject.Owner,
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
