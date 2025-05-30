using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.DataAccess.AuditTrail;

namespace Roxit.ZGW.Common.Web.Handlers;

public class LogAuditTrailGetObjectListCommandHandler : LogAuditTrailGetBaseHandler, IRequestHandler<LogAuditTrailGetObjectListCommand, CommandResult>
{
    private readonly IDbContextWithAuditTrail _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public LogAuditTrailGetObjectListCommandHandler(
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

    public async Task<CommandResult> Handle(LogAuditTrailGetObjectListCommand request, CancellationToken cancellationToken)
    {
        if (
            IsAudittrailRetrieveForRsin
            && IsAudittrailRecordRetrieveList
            && (
                request.RetrieveCatagory == RetrieveCatagory.Minimal
                || (request.RetrieveCatagory == RetrieveCatagory.All && !IsAudittrailRetrieveMinimal)
            )
        )
        {
            using var audittrail = _auditTrailFactory.Create(request.AuditTrailOptions);
            if (request.Page.HasValue && request.Count.HasValue)
            {
                await audittrail.GetListAsync(request.Count.Value, request.TotalCount, request.Page.Value, cancellationToken);
            }
            else
            {
                await audittrail.GetListAsync(request.TotalCount, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return new CommandResult(CommandStatus.OK);
    }
}

public class LogAuditTrailGetObjectListCommand : IRequest<CommandResult>
{
    public RetrieveCatagory RetrieveCatagory { get; set; }
    public int TotalCount { get; set; }
    public int? Count { get; set; }
    public int? Page { get; set; }
    public AuditTrailOptions AuditTrailOptions { get; set; }
}
