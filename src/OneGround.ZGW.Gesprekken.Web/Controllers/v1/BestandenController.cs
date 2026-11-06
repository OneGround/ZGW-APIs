using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Gesprekken.Contracts.v1.Requests;
using OneGround.ZGW.Gesprekken.Contracts.v1.Responses;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Authorization;
using OneGround.ZGW.Gesprekken.Web.Configuration;
using OneGround.ZGW.Gesprekken.Web.Contracts.v1;
using OneGround.ZGW.Gesprekken.Web.Handlers.v1;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Gesprekken.Web.Controllers.v1;

/// <summary>
/// Upload en download van bijlagen (patterns/file-upload): de client stuurt het bestand rechtstreeks naar de opslag.
/// </summary>
[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ProblemJson]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class BestandenController : ZGWControllerBase
{
    private readonly ApplicationConfiguration _applicationConfiguration;

    public BestandenController(
        ILogger<BestandenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IErrorResponseBuilder errorResponseBuilder,
        IConfiguration configuration
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>() ?? new ApplicationConfiguration();
    }

    /// <summary>
    /// Een upload starten: registreert het bestand en geeft een tijdelijke URL om het naartoe te sturen.
    /// </summary>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="413">Het bestand is te groot</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Bestanden.UploadSessie, Name = Operations.Bestanden.UploadSessie)]
    [Scope(AuthorizationScopes.Gesprekken.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(UploadSessieResponseDto))]
    public async Task<IActionResult> UploadSessieAsync([FromBody] UploadSessieRequestDto request)
    {
        if (request.Bestandsomvang > _applicationConfiguration.BijlageMaxBytes)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new ErrorResponse
                {
                    Code = "te-groot",
                    Title = "Het bestand is te groot.",
                    Status = StatusCodes.Status413PayloadTooLarge,
                }
            );
        }

        var result = await _mediator.Send(new CreateUploadSessieCommand { Bestand = _mapper.Map<Bestand>(request) });

        var bestand = _mapper.Map<BestandResponseDto>(result.Result.Bestand);
        var response = new UploadSessieResponseDto
        {
            FileId = bestand.FileId,
            UploadUrl = result.Result.UploadUrl,
            VerlooptOp = result.Result.Bestand.VerlooptOp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Bestand = bestand,
        };

        return Created(bestand.Url, response);
    }

    /// <summary>
    /// Een bestand opvragen, met een tijdelijke downloadUrl zodra het beschikbaar is.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Bestanden.Get, Name = Operations.Bestanden.Read)]
    [Scope(AuthorizationScopes.Gesprekken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BestandResponseDto))]
    public async Task<IActionResult> GetAsync(Guid fileId)
    {
        var result = await _mediator.Send(new GetBestandQuery { Id = fileId });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<BestandResponseDto>(result.Result.Bestand);
        response.DownloadUrl = result.Result.DownloadUrl;

        return Ok(response);
    }
}
