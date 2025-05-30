using System;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Documenten.Contracts.v1._1.Requests;
using Roxit.ZGW.Documenten.Contracts.v1._1.Responses;
using Roxit.ZGW.Documenten.Web.Authorization;
using Roxit.ZGW.Documenten.Web.Contracts.v1._1;
using Roxit.ZGW.Documenten.Web.Handlers.v1._1;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Documenten.Web.Controllers.v1._1;

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
    public async Task<IActionResult> UploadAsync([FromForm] BestandsDeelUploadRequestDto bestandsDeelUploadRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(UploadAsync), id);

        var result = await _mediator.Send(
            new UploadBestandsDeelCommand
            {
                BestandsDeelId = id,
                Inhoud = bestandsDeelUploadRequest.Inhoud,
                Lock = bestandsDeelUploadRequest.Lock,
            }
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
