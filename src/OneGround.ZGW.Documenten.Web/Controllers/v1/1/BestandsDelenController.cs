using System;
using System.Threading;
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
using OneGround.ZGW.Documenten.Contracts.v1._1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._1.Responses;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Contracts.v1._1;
using OneGround.ZGW.Documenten.Web.Handlers.v1._1;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Documenten.Web.Controllers.v1._1;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_1)]
[ZgwApiVersion(Api.LatestVersion_1_5)]
public class BestandsDelenController : ZGWControllerBase
{
    public BestandsDelenController(
        ILogger<BestandsDelenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder) { }

    // HTTP PUT http://documenten.user.local:5007/api/v1/bestandsdelen/59bad509-840b-4cd0-82dc-cbda74a75c2b

    /// <summary>
    /// Upload een bestandsdeel.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.BestandsDelen.Upload)]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [Scope(AuthorizationScopes.Documenten.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BestandsDeelResponseDto))]
    public async Task<IActionResult> UploadAsync(
        [FromForm] BestandsDeelUploadRequestDto bestandsDeelUploadRequest,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(UploadAsync), id);

        var result = await _mediator.Send(
            new UploadBestandsDeelCommand
            {
                BestandsDeelId = id,
                Inhoud = bestandsDeelUploadRequest.Inhoud,
                Lock = bestandsDeelUploadRequest.Lock,
            },
            cancellationToken
        );

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var bestandsDeelResponse = _mapper.Map<BestandsDeelResponseDto>(result.Result);

        return Ok(bestandsDeelResponse);
    }
}
