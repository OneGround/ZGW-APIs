using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Autorisaties.Common.BusinessRules;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Notificaties;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Autorisaties.Web.Handlers;

class UpdateApplicatieCommandHandler
    : AutorisatiesBaseHandler<UpdateApplicatieCommandHandler>,
        IRequestHandler<UpdateApplicatieCommand, CommandResult<Applicatie>>
{
    private readonly AcDbContext _context;
    private readonly IEntityUpdater<Applicatie> _entityUpdater;
    private readonly IApplicatieBusinessRuleService _applicatieBusinessRuleService;
    private readonly ICacheInvalidator _cacheInvalidator;

    public UpdateApplicatieCommandHandler(
        INotificatieService notificatieService,
        IConfiguration configuration,
        ILogger<UpdateApplicatieCommandHandler> logger,
        AcDbContext context,
        IEntityUriService uriService,
        IEntityUpdater<Applicatie> entityUpdater,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IApplicatieBusinessRuleService applicatieBusinessRuleService,
        ICacheInvalidator cacheInvalidator
    )
        : base(notificatieService, authorizationContextAccessor, uriService, configuration, logger)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _applicatieBusinessRuleService = applicatieBusinessRuleService;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<CommandResult<Applicatie>> Handle(UpdateApplicatieCommand request, CancellationToken cancellationToken)
    {
        var rsinFilter = GetRsinFilterPredicate<Applicatie>();

        var applicatie = await _context
            .Applicaties.Include(z => z.Autorisaties)
            .Include(z => z.ClientIds)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (applicatie == null)
        {
            return new CommandResult<Applicatie>(null, CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!await _applicatieBusinessRuleService.ValidateUpdateAsync(applicatie, request.Applicatie, errors))
        {
            return new CommandResult<Applicatie>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Updating Applicatie {Id}....", applicatie.Id);

        var clientIdsForInvalidation = request
            .Applicatie.ClientIds.Concat(applicatie.ClientIds)
            .Select(client => client.ClientId)
            .Distinct()
            .ToList();

        _entityUpdater.Update(request.Applicatie, applicatie);

        await _cacheInvalidator.InvalidateAsync(CacheEntity.Applicatie, clientIdsForInvalidation);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Applicatie {Id} successfully updated.", applicatie.Id);

        await SendNotificationAsync(Actie.update, applicatie, cancellationToken);

        return new CommandResult<Applicatie>(applicatie, CommandStatus.OK);
    }
}

class UpdateApplicatieCommand : IRequest<CommandResult<Applicatie>>
{
    public Guid Id { get; set; }
    public Applicatie Applicatie { get; set; }
}
