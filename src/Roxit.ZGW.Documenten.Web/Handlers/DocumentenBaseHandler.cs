using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.Configuration;
using Roxit.ZGW.Documenten.Web.Notificaties;

namespace Roxit.ZGW.Documenten.Web.Handlers;

public abstract class DocumentenBaseHandler<T> : ZGWBaseHandler
{
    protected readonly ILogger<T> _logger;
    protected readonly INotificatieService _notificatieService;
    protected readonly IEntityUriService _uriService;
    protected readonly ApplicationConfiguration _applicationConfiguration;
    protected readonly AuthorizationContext _authorizationContext;

    public DocumentenBaseHandler(
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

    public DocumentenBaseHandler(
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

    public Task SendNotificationAsync(Actie actie, EnkelvoudigInformatieObjectVersie informatieObjectVersie, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(informatieObjectVersie.EnkelvoudigInformatieObject);
        return SendNotificationAsync(
            Resource.enkelvoudiginformatieobject,
            actie,
            hoofdObject,
            hoofdObject,
            GetKenmerken(informatieObjectVersie),
            informatieObjectVersie.EnkelvoudigInformatieObject.Owner,
            cancellationToken
        );
    }

    public Task SendNotificationAsync(Actie actie, EnkelvoudigInformatieObject informatieObject, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(informatieObject);
        return SendNotificationAsync(
            Resource.enkelvoudiginformatieobject,
            actie,
            hoofdObject,
            hoofdObject,
            EmptyKenmerken,
            informatieObject.Owner,
            cancellationToken
        );
    }

    public Task SendNotificationAsync(Actie actie, GebruiksRecht entity, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(entity.InformatieObject);
        var resourceUrl = _uriService.GetUri(entity);
        return SendNotificationAsync(
            Resource.gebruiksrechten,
            actie,
            hoofdObject,
            resourceUrl,
            EmptyKenmerken,
            entity.InformatieObject.Owner,
            cancellationToken
        );
    }

    public Task SendNotificationAsync(Actie actie, Verzending entity, CancellationToken cancellationToken)
    {
        var hoofdObject = _uriService.GetUri(entity.InformatieObject);
        var resourceUrl = _uriService.GetUri(entity);
        return SendNotificationAsync(
            Resource.verzending,
            actie,
            hoofdObject,
            resourceUrl,
            EmptyKenmerken,
            entity.InformatieObject.Owner,
            cancellationToken
        );
    }

    private static ImmutableDictionary<string, string> EmptyKenmerken => ImmutableDictionary.Create<string, string>();

    private static Dictionary<string, string> GetKenmerken(EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObject)
    {
        return new Dictionary<string, string>
        {
            { "bronorganisatie", enkelvoudigInformatieObject.Bronorganisatie },
            { "informatieobjecttype", enkelvoudigInformatieObject.EnkelvoudigInformatieObject.InformatieObjectType },
            {
                "vertrouwelijkheidaanduiding",
                enkelvoudigInformatieObject.Vertrouwelijkheidaanduiding.HasValue ? $"{enkelvoudigInformatieObject.Vertrouwelijkheidaanduiding}" : null
            },
        };
    }

    private async Task SendNotificationAsync(
        Resource resource,
        Actie actie,
        string hoofdObject,
        string resourceUrl,
        IDictionary<string, string> kenmerken,
        string rsin,
        CancellationToken cancellationToken
    )
    {
        if (!_applicationConfiguration.DontSendNotificaties)
        {
            var notification = new Notification
            {
                Kanaal = Kanaal.documenten.ToString(),
                HoodfObject = hoofdObject,
                Resource = resource.ToString(),
                ResourceUrl = resourceUrl,
                Actie = actie.ToString(),
                Kenmerken = kenmerken,
                Rsin = rsin,
            };
            await _notificatieService.NotifyAsync(notification, cancellationToken);
        }
        else
        {
            _logger.LogDebug(
                "Warning: Notifications are disabled. Notification {NotificatieKanaal}-{NotificatieResource}-{NotificatieActie} will not be sent.",
                Kanaal.documenten,
                resource,
                actie
            );
        }
    }

    protected async Task LogConflictingValuesAsync(DbUpdateConcurrencyException ex)
    {
        try
        {
            var differences = new List<string>();

            foreach (var entry in ex.Entries)
            {
                var proposedValues = entry.CurrentValues;
                var databaseValues = await entry.GetDatabaseValuesAsync();

                if (databaseValues != null)
                {
                    foreach (var property in proposedValues.Properties)
                    {
                        var proposedValue = proposedValues[property];
                        var databaseValue = databaseValues[property];

                        // Log only the differences (keep in mind that the properties are of type object so we have to convert to string to get the ValueType instead of the object reference)
                        if (proposedValue?.ToString() != databaseValue?.ToString())
                        {
                            differences.Add($"{entry}: '{proposedValue}' -> '{databaseValue}'");
                        }
                    }
                }
            }
            if (differences.Count != 0)
            {
                _logger.LogInformation("DbUpdateConcurrencyException: Conflicting database- and pending changed values...");

                string error = string.Join(Environment.NewLine, differences);

                _logger.LogInformation("Error: {error}", error);
            }
        }
        catch
        {
            // ignore
        }
    }
}
