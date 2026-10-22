using System.Linq;
using System.Threading.Tasks;
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
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Contracts.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._7;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1._7;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_7)]
public class ZaaknummerReserverenController : ZGWControllerBase
{
    public ZaaknummerReserverenController(
        ILogger<ZaaknummerReserverenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, errorResponseBuilder) { }

    /// <summary>
    /// Reserveer een zaaknummer.
    /// Reserveer een zaaknummer binnen een specifieke bronorganisatie zonder direct een zaak aan te maken. Dit zaaknummer zal toegekend worden
    /// aan de eerstvolgende zaak die met dit zaaknummer wordt aangemaakt binnen de bronorganisatie. Zaaknummers die gereserveerd zijn (dus via
    /// dit endpoint worden teruggegeven), worden daarna nooit meer automatisch uitgegeven. Zaken kunnen enkel een gereserveerd nummer krijgen
    /// als ze dit nummer expliciet meegeven bij het aanmaken van een zaak.
    /// Als aantal niet wordt opgegeven of gelijk is aan 1, wordt een enkel object teruggegeven. Als aantal > 1, wordt een lijst van objecten
    /// teruggegeven.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="406">Not Acceptable</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaaknummerReserveren.Create, Name = Operations.ZaaknummerReserveren.Create)]
    [Scope(AuthorizationScopes.Zaken.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ReserveZaakIdentificatieResponseDto))]
    public async Task<IActionResult> ReserveAsync([FromBody] ReserveZaakIdentificatieRequestDto request)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(ReserveAsync), request);

        var result = await _mediator.Send(new ReserveZaakNummersCommand { Bronorganisatie = request.Bronorganisatie, Aantal = request.Aantal ?? 1 });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = result.Result.Select(nummer => new ReserveZaakIdentificatieResponseDto { Zaaknummer = nummer }).ToList();

        // Note: a single reservation gives a single object, more than one gives a list (as described by the standard)
        return StatusCode(StatusCodes.Status201Created, response.Count == 1 ? response[0] : response);
    }
}
