using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Common.Web.Authorization;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._1;

class PrioritizationDocumentJobCommandHandler : IRequestHandler<PrioritizationDocumentJobCommand, CommandResult>
{
    private readonly ILogger<PrioritizationDocumentJobCommandHandler> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IAuthorizationContextAccessor _authorizationContextAccessor;

    public PrioritizationDocumentJobCommandHandler(
        ILogger<PrioritizationDocumentJobCommandHandler> logger,
        IPublishEndpoint publishEndpoint,
        ICorrelationContextAccessor correlationContextAccessor,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _correlationContextAccessor = correlationContextAccessor;
        _authorizationContextAccessor = authorizationContextAccessor;
    }

    public async Task<CommandResult> Handle(PrioritizationDocumentJobCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Publishing {nameof} message for document {EnkelvoudigInformatieObjectId}",
            nameof(IDocumentPrioritizedJob),
            request.EnkelvoudigInformatieObjectId
        );

        var rsin = _authorizationContextAccessor.AuthorizationContext.Authorization.Rsin;

        await _publishEndpoint.Publish<IDocumentPrioritizedJob>(
            new
            {
                request.EnkelvoudigInformatieObjectId,
                _correlationContextAccessor.CorrelationId,
                Rsin = rsin,
            },
            cancellationToken
        );

        return new CommandResult(CommandStatus.OK);
    }
}

class PrioritizationDocumentJobCommand : IRequest<CommandResult>
{
    public Guid EnkelvoudigInformatieObjectId { get; internal set; }
}
