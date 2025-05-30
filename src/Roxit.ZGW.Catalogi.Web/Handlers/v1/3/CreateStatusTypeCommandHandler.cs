using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class CreateStatusTypeCommandHandler
    : CatalogiBaseHandler<CreateStatusTypeCommandHandler>,
        IRequestHandler<CreateStatusTypeCommand, CommandResult<StatusType>>
{
    private readonly ZtcDbContext _context;
    private readonly IEindStatusResolver _eindStatusResolver;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateStatusTypeCommandHandler(
        ILogger<CreateStatusTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IEindStatusResolver eindStatusResolver,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _eindStatusResolver = eindStatusResolver;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<StatusType>> Handle(CreateStatusTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating StatusType and validating....");

        var statusType = request.StatusType;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);

        var zaakTypeId = _uriService.GetId(request.ZaakType);
        var zaakType = await _context
            .ZaakTypen.Where(rsinFilter)
            .Include(z => z.Catalogus)
            .Include(z => z.StatusTypen)
            .SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{request.ZaakType}' niet gevonden.");
            return new CommandResult<StatusType>(null, CommandStatus.ValidationError, error);
        }

        if (!zaakType.Concept && zaakType.StatusTypen.Count != 0)
        {
            int maxVolgnummer = zaakType.StatusTypen.Max(s => s.VolgNummer);
            if (maxVolgnummer >= request.StatusType.VolgNummer)
            {
                var error = new ValidationError(
                    "volgnummer",
                    ErrorCode.Invalid,
                    $"Het statustype.volgnummer dient hoger te zijn dan het statustype met het hoogste volgnummer {maxVolgnummer} binnen het zaaktype."
                );
                return new CommandResult<StatusType>(null, CommandStatus.ValidationError, error);
            }
        }

        await _context.StatusTypen.AddAsync(statusType, cancellationToken);

        statusType.ZaakType = zaakType;

        statusType.Owner = statusType.ZaakType.Owner;
        // Note: Derive from Zaaktype instead of getting from request (decided to do so)
        statusType.BeginGeldigheid = zaakType.BeginGeldigheid;
        statusType.EindeGeldigheid = zaakType.EindeGeldigheid;
        statusType.BeginObject = zaakType.BeginObject;
        statusType.EindeObject = zaakType.EindeObject;
        // ----

        await AddVerplichteEigenschappen(request, statusType, errors, cancellationToken);

        if (errors.Count != 0)
        {
            return new CommandResult<StatusType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        await _cacheInvalidator.InvalidateAsync(statusType.ZaakType);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<StatusTypeResponseDto>(statusType);

            await audittrail.CreatedAsync(statusType.ZaakType, statusType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await _eindStatusResolver.ResolveAsync(statusType, cancellationToken);
        }
        _logger.LogDebug("StatusType {Id} successfully created.", statusType.Id);

        return new CommandResult<StatusType>(statusType, CommandStatus.OK);
    }

    private async Task AddVerplichteEigenschappen(
        CreateStatusTypeCommand request,
        StatusType statusType,
        List<ValidationError> errors,
        CancellationToken cancellationToken
    )
    {
        if (request.Eigenschappen == null)
        {
            return;
        }

        var eigenschapFilter = GetRsinFilterPredicate<Eigenschap>(t => t.Owner == _rsin);

        var verplichteEigenschappen = new List<StatusTypeVerplichteEigenschap>();
        foreach (var (url, index) in request.Eigenschappen.WithIndex())
        {
            var eigenschap = await _context
                .Eigenschappen.Where(eigenschapFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(url), cancellationToken);
            if (eigenschap == null)
            {
                var error = new ValidationError($"eigenschappen.{index}.url", ErrorCode.Invalid, $"Eigenschap {url} is onbekend.");
                errors.Add(error);
            }
            else
            {
                var verplichteEigenschap = new StatusTypeVerplichteEigenschap
                {
                    StatusType = statusType,
                    Eigenschap = eigenschap,
                    Owner = statusType.Owner,
                };
                verplichteEigenschappen.Add(verplichteEigenschap);
            }
        }

        if (errors.Count != 0)
        {
            return;
        }

        await _context.StatusTypeVerplichteEigenschappen.AddRangeAsync(verplichteEigenschappen, cancellationToken);

        await _cacheInvalidator.InvalidateAsync(verplichteEigenschappen.Select(t => t.Eigenschap), statusType.Owner);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "statustype" };
}

class CreateStatusTypeCommand : IRequest<CommandResult<StatusType>>
{
    public StatusType StatusType { get; internal set; }
    public string ZaakType { get; internal set; }
    public IEnumerable<string> Eigenschappen { get; internal set; }
}
