using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class CreateKlantContactCommandHandler
    : ZakenBaseHandler<CreateKlantContactCommandHandler>,
        IRequestHandler<CreateKlantContactCommand, CommandResult<KlantContact>>
{
    private readonly ZrcDbContext _zrcDbContext;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly INummerGenerator _nummerGenerator;

    public CreateKlantContactCommandHandler(
        ILogger<CreateKlantContactCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        ZrcDbContext zrcDbContext,
        IEntityUriService uriService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuditTrailFactory auditTrailFactory,
        INummerGenerator nummerGenerator,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _zrcDbContext = zrcDbContext;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
        _auditTrailFactory = auditTrailFactory;
        _nummerGenerator = nummerGenerator;
    }

    public async Task<CommandResult<KlantContact>> Handle(CreateKlantContactCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating KlantContact and validating....");

        var klantContact = request.KlantContact;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _zrcDbContext
            .Zaken.Where(rsinFilter)
            .Include(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend.");
            errors.Add(error);
            return new CommandResult<KlantContact>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaak, errors))
        {
            return new CommandResult<KlantContact>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        if (string.IsNullOrEmpty(klantContact.Identificatie))
        {
            var rsin = zaak.Bronorganisatie;

            var klantcontactennummer = await _nummerGenerator.GenerateAsync(rsin, "klantcontacten", cancellationToken);

            klantContact.Identificatie = klantcontactennummer;
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            await _zrcDbContext.KlantContacten.AddAsync(klantContact, cancellationToken);

            klantContact.Zaak = zaak;

            audittrail.SetNew<KlantContactResponseDto>(klantContact);

            await audittrail.CreatedAsync(klantContact.Zaak, klantContact, cancellationToken);

            await _zrcDbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("KlantContact {Id} successfully created.", klantContact.Id);

        await SendNotificationAsync(Actie.create, klantContact, cancellationToken);

        return new CommandResult<KlantContact>(klantContact, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "klantcontact" };
}

public class CreateKlantContactCommand : IRequest<CommandResult<KlantContact>>
{
    public KlantContact KlantContact { get; internal set; }
    public string ZaakUrl { get; internal set; }
}
