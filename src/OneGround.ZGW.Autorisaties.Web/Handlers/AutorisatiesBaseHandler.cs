using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Configuration;
using OneGround.ZGW.Autorisaties.Web.Notificaties;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Autorisaties.Web.Handlers;

public abstract class AutorisatiesBaseHandler<T> : ZGWBaseHandler
{
    protected readonly ILogger<T> _logger;
    private readonly INotificatieService _notificatieService;
    private readonly IEntityUriService _uriService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public AutorisatiesBaseHandler(
        INotificatieService notificatieService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IEntityUriService uriService,
        IConfiguration configuration,
        ILogger<T> logger
    )
        : base(configuration, authorizationContextAccessor)
    {
        _notificatieService = notificatieService;
        _uriService = uriService;
        _logger = logger;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        if (_applicationConfiguration == null)
            throw new InvalidOperationException("Application section not found in appsettings.");
    }

    public async Task SendNotificationAsync(Actie actie, Applicatie applicatie, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(applicatie);
        var notification = new Notification
        {
            Kanaal = Kanaal.autorisaties.ToString(),
            HoodfObject = hoofdObject,
            Resource = Resource.applicatie.ToString(),
            ResourceUrl = hoofdObject,
            Actie = actie.ToString(),
            Kenmerken = ImmutableDictionary.Create<string, string>(),
            Rsin = _rsin,
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
}
