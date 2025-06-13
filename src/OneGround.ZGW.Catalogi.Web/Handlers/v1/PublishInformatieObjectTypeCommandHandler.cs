using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class PublishInformatieObjectTypeCommandHandler
    : CatalogiBaseHandler<PublishInformatieObjectTypeCommandHandler>,
        IRequestHandler<PublishInformatieObjectTypeCommand, CommandResult<InformatieObjectType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public PublishInformatieObjectTypeCommandHandler(
        ILogger<PublishInformatieObjectTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<InformatieObjectType>> Handle(PublishInformatieObjectTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting InformatieObjectType publish process.");

        var rsinFilter = GetRsinFilterPredicate<InformatieObjectType>(b => b.Catalogus.Owner == _rsin);

        var informatieObjectType = await _context
            .InformatieObjectTypen.AsSplitQuery()
            .Where(rsinFilter)
            .Include(z => z.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (informatieObjectType == null)
        {
            return new CommandResult<InformatieObjectType>(null, CommandStatus.NotFound);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<InformatieObjectTypeResponseDto>(informatieObjectType);

            informatieObjectType.Concept = false;

            var errors = new List<ValidationError>();

            var informatieObjectTypen = await _context
                .InformatieObjectTypen.Include(c => c.Catalogus)
                .Where(rsinFilter)
                .Where(z => z.CatalogusId == informatieObjectType.CatalogusId)
                .Where(z => z.Id != informatieObjectType.Id && z.Omschrijving == informatieObjectType.Omschrijving)
                .ToListAsync(cancellationToken);
            if (!_conceptBusinessRule.ValidateGeldigheid(informatieObjectTypen.OfType<IConceptEntity>().ToList(), informatieObjectType, errors))
            {
                var error = new ValidationError(
                    "informatieobjecttype",
                    ErrorCode.Invalid,
                    $"Kan InformatieObjectType niet publiceren omdat de omschrijving '{informatieObjectType.Omschrijving}' al gebruikt is binnen de geldigheidsperiode."
                );
                return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, error);
            }

            audittrail.SetNew<InformatieObjectTypeResponseDto>(informatieObjectType);

            await audittrail.PatchedAsync(
                informatieObjectType.Catalogus,
                informatieObjectType,
                "Informatieobjecttype gepubliceerd",
                cancellationToken
            );

            await _cacheInvalidator.InvalidateAsync(informatieObjectType);

            await _context.SaveChangesAsync(cancellationToken);
        }

        return new CommandResult<InformatieObjectType>(informatieObjectType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "informatieobjecttype" };
}

class PublishInformatieObjectTypeCommand : IRequest<CommandResult<InformatieObjectType>>
{
    public Guid Id { get; set; }
}
