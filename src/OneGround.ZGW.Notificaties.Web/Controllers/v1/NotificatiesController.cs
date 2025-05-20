using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.Authorization;
using OneGround.ZGW.Notificaties.Web.Handlers;
using Swashbuckle.AspNetCore.Annotations;

//
// Bron NRC API: https://notificaties-api.vng.cloud/api/v1/schema/
// Bron overig:  https://vng-realisatie.github.io/gemma-zaken/themas/achtergronddocumentatie/notificaties
// Bron overig:  https://vng-realisatie.github.io/gemma-zaken/standaard/notificaties-consumer/index
// Bron overig:  https://vng-realisatie.github.io/gemma-zaken/ontwikkelaars/handleidingen-en-tutorials/notificeren

namespace OneGround.ZGW.Notificaties.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class NotificatiesController : ZGWControllerBase
{
    public NotificatiesController(
        ILogger<NotificatiesController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder) { }

    /// <summary>
    /// Publiceer een notificatie.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Notificaties.Create, Name = Operations.Notificaties.Create)]
    [Scope(AuthorizationScopes.Notificaties.Produce)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(NotificatieDto))]
    public async Task<IActionResult> NotificeerAsync([FromBody] NotificatieDto notificatieRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(NotificeerAsync), notificatieRequest);

        var notificatie = _mapper.Map<Notificatie>(notificatieRequest);

        var result = await _mediator.Send(new QueueNotificatieCommand { Notificatie = notificatie });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        // Note: Response has the same content as request so mapping is not necessary!
        return Ok(notificatieRequest);
    }
}
