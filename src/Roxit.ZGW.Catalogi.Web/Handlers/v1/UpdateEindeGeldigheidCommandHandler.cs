using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

public class UpdateEindeGeldigheidCommandHandler
    : CatalogiBaseHandler<UpdateEindeGeldigheidCommandHandler>,
        IRequestHandler<UpdateEindeGeldigheidCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateEindeGeldigheidCommandHandler(
        ILogger<UpdateEindeGeldigheidCommandHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(UpdateEindeGeldigheidCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating EindeGeldigheid {Id}....", request.Entity.Id);

        var errors = new List<ValidationError>();

        switch (request.Entity)
        {
            case ZaakType zaakType:
                var catalogusZt = await _context
                    .Catalogussen.AsNoTracking()
                    .Include(c => c.ZaakTypes)
                    .SingleOrDefaultAsync(c => c.Id == zaakType.Catalogus.Id, cancellationToken);

                using (var audittrail = _auditTrailFactory.Create(new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype" }))
                {
                    if (
                        !_conceptBusinessRule.ValidateGeldigheid(
                            catalogusZt
                                .ZaakTypes.Where(t => t.Id != zaakType.Id && t.Identificatie == zaakType.Identificatie)
                                .OfType<IConceptEntity>()
                                .ToList(),
                            new ZaakType
                            {
                                BeginGeldigheid = zaakType.BeginGeldigheid,
                                EindeGeldigheid = zaakType.EindeGeldigheid,
                                Concept = zaakType.Concept,
                            },
                            errors
                        )
                    )
                    {
                        var error = new ValidationError(
                            "zaaktype",
                            ErrorCode.Invalid,
                            $"Zaaktype identificatie '{zaakType.Identificatie}' is al gebruikt binnen de geldigheidsperiode."
                        );
                        return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
                    }
                    audittrail.SetOld<ZaakTypeResponseDto>(zaakType);
                    UpdateEindeGeldigheid(zaakType);
                    audittrail.SetNew<ZaakTypeResponseDto>(zaakType);
                    await _cacheInvalidator.InvalidateAsync(zaakType);
                    break;
                }

            case InformatieObjectType informatieObjectType:
                var catalogusIot = await _context
                    .Catalogussen.AsNoTracking()
                    .Include(c => c.InformatieObjectTypes)
                    .SingleOrDefaultAsync(c => c.Id == informatieObjectType.Catalogus.Id, cancellationToken);

                using (
                    var audittrail = _auditTrailFactory.Create(
                        new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "informatieobjecttype" }
                    )
                )
                {
                    if (
                        !_conceptBusinessRule.ValidateGeldigheid(
                            catalogusIot
                                .InformatieObjectTypes.Where(t =>
                                    t.Id != informatieObjectType.Id && t.Omschrijving == informatieObjectType.Omschrijving
                                )
                                .OfType<IConceptEntity>()
                                .ToList(),
                            new InformatieObjectType
                            {
                                BeginGeldigheid = informatieObjectType.BeginGeldigheid,
                                EindeGeldigheid = informatieObjectType.EindeGeldigheid,
                                Concept = informatieObjectType.Concept,
                            },
                            errors
                        )
                    )
                    {
                        var error = new ValidationError(
                            "informatieobjecttype",
                            ErrorCode.Invalid,
                            $"Informatieobjecttype omschrijving '{informatieObjectType.Omschrijving}' is al gebruikt binnen de geldigheidsperiode."
                        );
                        return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
                    }

                    audittrail.SetOld<InformatieObjectTypeResponseDto>(informatieObjectType);
                    UpdateEindeGeldigheid(informatieObjectType);
                    audittrail.SetNew<InformatieObjectTypeResponseDto>(informatieObjectType);
                    await _cacheInvalidator.InvalidateAsync(informatieObjectType);
                    break;
                }

            case BesluitType besluitObjectType:
                var catalogusBt = await _context
                    .Catalogussen.AsNoTracking()
                    .Include(c => c.BesluitTypes)
                    .SingleOrDefaultAsync(c => c.Id == besluitObjectType.Catalogus.Id, cancellationToken);

                using (var audittrail = _auditTrailFactory.Create(new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "besluittype" }))
                {
                    if (
                        !_conceptBusinessRule.ValidateGeldigheid(
                            catalogusBt
                                .BesluitTypes.Where(t => t.Id != besluitObjectType.Id && t.Omschrijving == besluitObjectType.Omschrijving)
                                .OfType<IConceptEntity>()
                                .ToList(),
                            new BesluitType
                            {
                                BeginGeldigheid = besluitObjectType.BeginGeldigheid,
                                EindeGeldigheid = besluitObjectType.EindeGeldigheid,
                                Concept = besluitObjectType.Concept,
                            },
                            errors
                        )
                    )
                    {
                        var error = new ValidationError(
                            "besluittype",
                            ErrorCode.Invalid,
                            $"Besluittype omschrijving '{besluitObjectType.Omschrijving}' is al gebruikt binnen de geldigheidsperiode."
                        );
                        return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
                    }

                    audittrail.SetOld<BesluitTypeResponseDto>(besluitObjectType);
                    UpdateEindeGeldigheid(besluitObjectType);
                    audittrail.SetNew<BesluitTypeResponseDto>(besluitObjectType);
                    await _cacheInvalidator.InvalidateAsync(besluitObjectType);
                    break;
                }

            default:
                throw new ArgumentException($"Unsupported entity {request.Entity.GetType()}.");
        }

        if (errors.Count != 0)
        {
            return new CommandResult(CommandStatus.ValidationError, errors.ToArray());
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("EindeGeldigheid successfully updated.");

        return new CommandResult(CommandStatus.OK);
    }

    private void UpdateEindeGeldigheid<T>(T entity)
        where T : class, IValidityEntity, new()
    {
        _context.Attach(new T { Id = entity.Id, EindeGeldigheid = entity.EindeGeldigheid }).Property(o => o.EindeGeldigheid).IsModified = true;
    }
}

public class UpdateEindeGeldigheidCommand : IRequest<CommandResult>
{
    public IValidityEntity Entity { get; internal set; }
}
