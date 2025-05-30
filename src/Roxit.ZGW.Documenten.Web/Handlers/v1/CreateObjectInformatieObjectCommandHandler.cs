using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Contracts.v1.Responses;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.BusinessRules.v1;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1;

class CreateObjectInformatieObjectCommandHandler
    : DocumentenBaseHandler<CreateObjectInformatieObjectCommandHandler>,
        IRequestHandler<CreateObjectInformatieObjectCommand, CommandResult<ObjectInformatieObject>>
{
    private readonly DrcDbContext _context;
    private readonly IObjectInformatieObjectBusinessRuleService _objectInformatieObjectBusinessRuleService;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateObjectInformatieObjectCommandHandler(
        ILogger<CreateObjectInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IObjectInformatieObjectBusinessRuleService objectInformatieObjectBusinessRuleService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _objectInformatieObjectBusinessRuleService = objectInformatieObjectBusinessRuleService;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ObjectInformatieObject>> Handle(CreateObjectInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ObjectInformatieObject....");

        var objectInformatieObject = request.ObjectInformatieObject;

        var errors = new List<ValidationError>();

        await _objectInformatieObjectBusinessRuleService.ValidateAsync(
            objectInformatieObject,
            request.InformatieObjectUrl,
            _applicationConfiguration.IgnoreZaakAndBesluitValidation,
            errors,
            cancellationToken
        );

        if (errors.Count != 0)
        {
            return new CommandResult<ObjectInformatieObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var informatieObject = await _context
            .EnkelvoudigInformatieObjecten.Where(rsinFilter)
            .Include(z => z.ObjectInformatieObjecten)
            .SingleOrDefaultAsync(e => e.Id == _uriService.GetId(request.InformatieObjectUrl), cancellationToken);

        if (informatieObject == null)
        {
            var error = new ValidationError("informatieobject", ErrorCode.ObjectDoesNotExist, "Het object bestaat niet in de database.");
            return new CommandResult<ObjectInformatieObject>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            objectInformatieObject.InformatieObjectId = informatieObject.Id;
            objectInformatieObject.InformatieObject = informatieObject;
            objectInformatieObject.Owner = informatieObject.Owner;

            await _context.ObjectInformatieObjecten.AddAsync(objectInformatieObject, cancellationToken); // Note: Sequential Guid for Id is generated here by the DBMS

            audittrail.SetNew<ObjectInformatieObjectResponseDto>(objectInformatieObject);

            await audittrail.CreatedAsync(objectInformatieObject.InformatieObject, objectInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ObjectInformatieObject {Id} successfully created.", objectInformatieObject.Id);
        }

        return new CommandResult<ObjectInformatieObject>(objectInformatieObject, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "objectinformatieobject" };
}

class CreateObjectInformatieObjectCommand : IRequest<CommandResult<ObjectInformatieObject>>
{
    public ObjectInformatieObject ObjectInformatieObject { get; internal set; }
    public string InformatieObjectUrl { get; internal set; }
}
