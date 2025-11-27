using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Documenten.Messaging.Handlers;
using OneGround.ZGW.Documenten.Messaging.Helper;
using OneGround.ZGW.Notificaties.Contracts.v1;
using Headers = OneGround.ZGW.Common.Constants.Headers;

namespace OneGround.ZGW.Documenten.Messaging.Controllers;

[Route("api/v1")]
[Authorize]
public class NotificatieController : Controller
{
    private readonly IMediator _mediator;
    private readonly IErrorResponseBuilder _errorResponseBuilder;

    public NotificatieController(
        ILogger<NotificatieController> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IMediator mediator,
        IErrorResponseBuilder errorResponseBuilder
    )
    {
        _mediator = mediator;
        _errorResponseBuilder = errorResponseBuilder;
    }

    [HttpPost("notificatie/{rsin}")]
    public async Task<IActionResult> ReceiveNotificatie([FromBody] NotificatieDto notificatie, string rsin, CancellationToken cancellationToken)
    {
        if (rsin != RsinFromClaims)
            return _errorResponseBuilder.Forbidden();

        // We are interested in zaak- or besluitinformatieobject-resource only (and create- and destroy-action notifications)
        if (notificatie.Resource == "zaakinformatieobject" || notificatie.Resource == "besluitinformatieobject")
        {
            var correlationId = GetCorrelationIdFromRequestHeaders();
            var batchId = GetBatchIdFromRequestHeadersOrDefault();

            var informatieObjectKenmerk = InformatieObjectParser.ParseInformatieObject(notificatie.Kenmerken);

            if (notificatie.Actie == "create")
            {
                var command = new AddObjectInformatieObjectCommand
                {
                    Rsin = rsin,
                    CorrelationId = correlationId,
                    BatchId = batchId,
                    Object = notificatie.HoofdObject,
                    InformatieObjectKenmerk = informatieObjectKenmerk,
                };

                var result = await _mediator.Send(command, cancellationToken);

                return result.Status switch
                {
                    CommandStatus.ValidationError => _errorResponseBuilder.BadRequest(result.Errors),
                    CommandStatus.OK => Ok(new { Message = "Synchronization of objectinformatieobject to DRC successfully queued." }),
                    _ => _errorResponseBuilder.InternalServerError("An unexpected error occurred while processing the command."),
                };
            }
            else if (notificatie.Actie == "destroy")
            {
                var command = new DeleteObjectInformatieObjectCommand
                {
                    Rsin = rsin,
                    CorrelationId = correlationId,
                    BatchId = batchId,
                    Object = notificatie.HoofdObject,
                    InformatieObjectKenmerk = informatieObjectKenmerk,
                };

                var result = await _mediator.Send(command, cancellationToken);

                return result.Status switch
                {
                    CommandStatus.ValidationError => _errorResponseBuilder.BadRequest(result.Errors),
                    CommandStatus.OK => Ok(new { Message = "Synchronization of objectinformatieobject from DRC successfully queued." }),
                    _ => _errorResponseBuilder.InternalServerError("An unexpected error occurred while processing the command."),
                };
            }
        }

        return Ok(new { Message = "Notification handling skipped" });
    }

    private Guid GetCorrelationIdFromRequestHeaders()
    {
        if (
            HttpContext.Request.Headers.TryGetValue(Headers.CorrelationHeader, out var headerValue)
            && Guid.TryParse(headerValue, out var correlationId)
        )
        {
            return correlationId;
        }
        return Guid.NewGuid();
    }

    private Guid? GetBatchIdFromRequestHeadersOrDefault()
    {
        if (HttpContext.Request.Headers.TryGetValue(Headers.BatchId, out var headerValue) && Guid.TryParse(headerValue, out var batchId))
        {
            return batchId;
        }
        return null;
    }

    private string RsinFromClaims => User.Claims.FirstOrDefault(c => c.Type.Equals("rsin", StringComparison.OrdinalIgnoreCase))?.Value;
}
