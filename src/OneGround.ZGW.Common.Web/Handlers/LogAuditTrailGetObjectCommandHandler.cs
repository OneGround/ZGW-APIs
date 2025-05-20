using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;

namespace OneGround.ZGW.Common.Web.Handlers;

public class LogAuditTrailGetObjectCommandHandler : LogAuditTrailGetBaseHandler, IRequestHandler<LogAuditTrailGetObjectCommand, CommandResult>
{
    private readonly IDbContextWithAuditTrail _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public LogAuditTrailGetObjectCommandHandler(
        IConfiguration configuration,
        IDbContextWithAuditTrail context,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(LogAuditTrailGetObjectCommand request, CancellationToken cancellationToken)
    {
        if (
            IsAudittrailRetrieveForRsin
            && (
                request.RetrieveCatagory == RetrieveCatagory.Minimal
                || (request.RetrieveCatagory == RetrieveCatagory.All && !IsAudittrailRetrieveMinimal)
            )
        )
        {
            using var audittrail = _auditTrailFactory.Create(request.AuditTrailOptions);
            await audittrail.GetAsync(request.BaseEntity, request.SubEntity, overruleActieWeergave: request.OverruleActieWeergave, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
        return new CommandResult(CommandStatus.OK);
    }
}

public class LogAuditTrailGetObjectCommand : IRequest<CommandResult>
{
    public RetrieveCatagory RetrieveCatagory { get; set; }
    public IBaseEntity BaseEntity { get; set; }
    public IUrlEntity SubEntity { get; set; }
    public string OverruleActieWeergave { get; set; }
    public AuditTrailOptions AuditTrailOptions { get; set; }
}
