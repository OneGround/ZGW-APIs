﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Autorisaties.Common.BusinessRules;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Notificaties;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Autorisaties.Web.Handlers;

class CreateApplicatieCommandHandler
    : AutorisatiesBaseHandler<CreateApplicatieCommandHandler>,
        IRequestHandler<CreateApplicatieCommand, CommandResult<Applicatie>>
{
    private readonly AcDbContext _context;
    private readonly IApplicatieBusinessRuleService _applicatieBusinessRuleService;

    public CreateApplicatieCommandHandler(
        INotificatieService notificatieService,
        IEntityUriService uriService,
        IConfiguration configuration,
        ILogger<CreateApplicatieCommandHandler> logger,
        AcDbContext context,
        IApplicatieBusinessRuleService applicatieBusinessRuleService,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(notificatieService, authorizationContextAccessor, uriService, configuration, logger)
    {
        _context = context;
        _applicatieBusinessRuleService = applicatieBusinessRuleService;
    }

    public async Task<CommandResult<Applicatie>> Handle(CreateApplicatieCommand request, CancellationToken cancellationToken)
    {
        var applicatie = request.Applicatie;

        var errors = new List<ValidationError>();

        if (!await _applicatieBusinessRuleService.ValidateAddAsync(applicatie, errors))
        {
            return new CommandResult<Applicatie>(null, CommandStatus.ValidationError, errors.ToArray());
        }
        await _context.Applicaties.AddAsync(applicatie, cancellationToken);

        applicatie.Owner = _rsin;

        applicatie.Autorisaties.ForEach(a => a.Owner = _rsin);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Applicatie {Id} successfully created.", applicatie.Id);

        await SendNotificationAsync(Actie.create, applicatie, cancellationToken);

        return new CommandResult<Applicatie>(applicatie, CommandStatus.OK);
    }
}

class CreateApplicatieCommand : IRequest<CommandResult<Applicatie>>
{
    public Applicatie Applicatie { get; set; }
}
