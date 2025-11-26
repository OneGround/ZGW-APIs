using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.BusinessRules;
using OneGround.ZGW.Besluiten.Web.Notificaties;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class CreateBesluitInformatieObjectCommandHandler
    : BesluitenBaseHandler<CreateBesluitInformatieObjectCommandHandler>,
        IRequestHandler<CreateBesluitInformatieObjectCommand, CommandResult<BesluitInformatieObject>>
{
    private readonly BrcDbContext _context;
    private readonly IBesluitInformatieObjectBusinessRuleService _besluitInformatieObjectBusinessRuleService;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateBesluitInformatieObjectCommandHandler(
        ILogger<CreateBesluitInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        BrcDbContext context,
        IEntityUriService uriService,
        IBesluitInformatieObjectBusinessRuleService besluitInformatieObjectBusinessRuleService,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitKenmerkenResolver besluitKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, besluitKenmerkenResolver)
    {
        _context = context;
        _besluitInformatieObjectBusinessRuleService = besluitInformatieObjectBusinessRuleService;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<BesluitInformatieObject>> Handle(
        CreateBesluitInformatieObjectCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Creating BesluitInformatieObject and validating....");

        var besluitInformatieObject = request.BesluitInformatieObject;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Besluit>();

        var besluit = await _context
            .Besluiten.Where(rsinFilter)
            .Include(z => z.BesluitInformatieObjecten)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.BesluitUrl), cancellationToken);

        if (besluit == null)
        {
            var error = new ValidationError("besluit", ErrorCode.Invalid, $"Besluit {request.BesluitUrl} is onbekend.");

            errors.Add(error);
        }
        else
        {
            await _besluitInformatieObjectBusinessRuleService.ValidateAsync(
                besluit,
                besluitInformatieObject,
                _applicationConfiguration.IgnoreInformatieObjectValidation,
                errors
            );
        }

        if (errors.Count != 0)
        {
            return new CommandResult<BesluitInformatieObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            besluitInformatieObject.BesluitId = besluit.Id;
            besluitInformatieObject.Besluit = besluit;
            besluitInformatieObject.Registratiedatum = DateOnly.FromDateTime(DateTime.Today);
            besluitInformatieObject.AardRelatie = AardReleatie.legt_vast; // Zetten van relatieinformatie op BesluitInformatieObject - resource(brc-004)
            besluitInformatieObject.Owner = besluit.Owner;

            await _context.BesluitInformatieObjecten.AddAsync(besluitInformatieObject, cancellationToken);

            audittrail.SetNew<BesluitInformatieObjectResponseDto>(besluitInformatieObject);

            await audittrail.CreatedAsync(besluitInformatieObject.Besluit, besluitInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("BesluitInformatieObject {Id} successfully created.", besluitInformatieObject.Id);
        }

        var extraKenmerken = new Dictionary<string, string>
        {
            { "besluitinformatieobject.informatieobject", besluitInformatieObject.InformatieObject },
        };

        await SendNotificationAsync(Actie.create, besluitInformatieObject, extraKenmerken, cancellationToken);

        return new CommandResult<BesluitInformatieObject>(besluitInformatieObject, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluitinformatieobject" };
}

class CreateBesluitInformatieObjectCommand : IRequest<CommandResult<BesluitInformatieObject>>
{
    public BesluitInformatieObject BesluitInformatieObject { get; internal set; }
    public string BesluitUrl { get; internal set; }
}
