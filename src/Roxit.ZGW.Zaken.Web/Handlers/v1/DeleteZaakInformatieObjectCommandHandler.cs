using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Batching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.CorrelationId;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Messaging;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Messaging.Contracts;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class DeleteZaakInformatieObjectCommandHandler
    : ZakenBaseHandler<DeleteZaakInformatieObjectCommandHandler>,
        IRequestHandler<DeleteZaakInformatieObjectCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IRequestClient<IDeleteObjectInformatieObject> _client;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IBatchIdAccessor _batchIdAccessor;

    public DeleteZaakInformatieObjectCommandHandler(
        ILogger<DeleteZaakInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        INotificatieService notificatieService,
        IRequestClient<IDeleteObjectInformatieObject> client,
        IPublishEndpoint publishEndpoint,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        ICorrelationContextAccessor correlationContextAccessor,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBatchIdAccessor batchIdAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _client = client;
        _publishEndpoint = publishEndpoint;
        _auditTrailFactory = auditTrailFactory;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
        _correlationContextAccessor = correlationContextAccessor;
        _batchIdAccessor = batchIdAccessor;
    }

    public async Task<CommandResult> Handle(DeleteZaakInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakInformatieObject>();

        var zaakInformatieObject = await _context
            .ZaakInformatieObjecten.Where(rsinFilter)
            .Include(z => z.Zaak)
            .ThenInclude(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakInformatieObject == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakInformatieObject.Zaak, errors))
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        // keep the reference for later, because saving context removed relation from zaakInformatieObject.Zaak
        var zaak = zaakInformatieObject.Zaak;
        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ZaakInformatieObject {Id}....", zaakInformatieObject.Id);

            audittrail.SetOld<ZaakInformatieObjectResponseDto>(zaakInformatieObject);

            _context.ZaakInformatieObjecten.Remove(zaakInformatieObject);

            await audittrail.DestroyedAsync(zaakInformatieObject.Zaak, zaakInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakInformatieObject {Id} successfully deleted.", zaakInformatieObject.Id);
        }

        await SynchronizeObjectInformatieObjectInDrc(zaak, zaakInformatieObject, cancellationToken);

        await SendNotificationAsync(Actie.destroy, zaakInformatieObject, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private async Task SynchronizeObjectInformatieObjectInDrc(
        Zaak zaak,
        ZaakInformatieObject zaakInformatieObject,
        CancellationToken cancellationToken
    )
    {
        bool asyncOnly = _applicationConfiguration.DrcSynchronizationAsyncOnlyMode;

        //
        // Note: The mirrored relationship in the Documents API is removed by the Business API. Consumers cannot do this manually.

        bool timeout = false;
        if (!asyncOnly && string.IsNullOrEmpty(_batchIdAccessor.Id))
        {
            // Only non-batch messages should be called synchroneously
            try
            {
                // Non-batch messages should be handled first so try to handle synchroneously
                // Try to get ObjectInformatieObject synchronized into the DRC
                var response = await _client.GetResponse<DeleteObjectInformatieObjectResult>(
                    new
                    {
                        Object = _uriService.GetUri(zaak),
                        ObjectDestroy = true,
                        zaakInformatieObject.InformatieObject,
                        Rsin = _rsin,
                        _correlationContextAccessor.CorrelationId,
                    },
                    timeout: _applicationConfiguration.DrcSynchronizationTimeoutSeconds * 1000
                );

                _logger.LogDebug(
                    "Successfully removed the mirrored relationship from DRC. Url was {ObjectInformatieObjectUrl}",
                    response.Message.ObjectInformatieObjectUrl
                );
                return;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout error removing the mirrored relationship from DRC. Message is put on the queue and processed later.");

                timeout = true;
            }
        }

        if (!string.IsNullOrEmpty(_batchIdAccessor.Id) || asyncOnly || timeout)
        {
            byte priority = (byte)(string.IsNullOrEmpty(_batchIdAccessor.Id) ? MessagePriority.Normal : MessagePriority.Low);

            try
            {
                // Handle it a-synchroneously in case of batch messages or in case of an error (timeout)
                await _publishEndpoint.Publish<IDeleteObjectInformatieObject>(
                    new
                    {
                        Object = _uriService.GetUri(zaak),
                        ObjectDestroy = true,
                        zaakInformatieObject.InformatieObject,
                        Rsin = _rsin,
                        _correlationContextAccessor.CorrelationId,
                    },
                    context => context.SetPriority(priority),
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                // TEST issue
                _logger.LogError(ex, "Publishing: Error removing the mirrored relationship from DRC.");
            }
        }
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakinformatieobject" };
}

class DeleteZaakInformatieObjectCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
