using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
using OneGround.ZGW.Zaken.Web.Handlers;

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
            EnkelvoudigInformatieObject => Resource.enkelvoudiginformatieobject, // ???
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

    public async Task SendNotificationAsync<K>(Actie actie, K informatieObjectEntity, CancellationToken cancellationToken)
        where K : IInformatieObjectEntity, IUrlEntity
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

    //[Obsolete("USe the Generic one")]
    //public Task SendNotificationAsync(Actie actie, EnkelvoudigInformatieObjectVersie informatieObjectVersie, CancellationToken cancellationToken)
    //{
    //    var hoofdObject = _uriService.GetUri(informatieObjectVersie.InformatieObject);
    //    return SendNotificationAsync(
    //        Resource.enkelvoudiginformatieobject,
    //        actie,
    //        hoofdObject,
    //        hoofdObject,
    //        GetKenmerken(informatieObjectVersie),
    //        informatieObjectVersie.InformatieObject.Owner,
    //        cancellationToken
    //    );
    //}

    //[Obsolete("USe the Generic one")]
    //public Task SendNotificationAsync(Actie actie, GebruiksRecht entity, CancellationToken cancellationToken)
    //{
    //    var hoofdObject = _uriService.GetUri(entity.InformatieObject);
    //    var resourceUrl = _uriService.GetUri(entity);
    //    return SendNotificationAsync(
    //        Resource.gebruiksrechten,
    //        actie,
    //        hoofdObject,
    //        resourceUrl,
    //        EmptyKenmerken,
    //        entity.InformatieObject.Owner,
    //        cancellationToken
    //    );
    //}

    //[Obsolete("USe the Generic one")]
    //public Task SendNotificationAsync(Actie actie, Verzending entity, CancellationToken cancellationToken)
    //{
    //    var hoofdObject = _uriService.GetUri(entity.InformatieObject);
    //    var resourceUrl = _uriService.GetUri(entity);
    //    return SendNotificationAsync(
    //        Resource.verzending,
    //        actie,
    //        hoofdObject,
    //        resourceUrl,
    //        EmptyKenmerken,
    //        entity.InformatieObject.Owner,
    //        cancellationToken
    //    );
    //}

    //private static ImmutableDictionary<string, string> EmptyKenmerken => ImmutableDictionary.Create<string, string>();

    private static Dictionary<string, string> GetKenmerken(EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObject)
    {
        return new Dictionary<string, string>
        {
            { "bronorganisatie", enkelvoudigInformatieObject.Bronorganisatie },
            { "informatieobjecttype", enkelvoudigInformatieObject.InformatieObject.InformatieObjectType },
            {
                "vertrouwelijkheidaanduiding",
                enkelvoudigInformatieObject.Vertrouwelijkheidaanduiding.HasValue ? $"{enkelvoudigInformatieObject.Vertrouwelijkheidaanduiding}" : null
            },
        };
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
