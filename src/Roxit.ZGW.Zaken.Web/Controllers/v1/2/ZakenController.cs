using System;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Zaken.Contracts.v1.Requests;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Handlers.v1;
using Roxit.ZGW.Zaken.Web.Handlers.v1._2;
using Swashbuckle.AspNetCore.Annotations;

//
// Bron ZRC API:       https://zaken-api.vng.cloud/api/v1/schema/
// Bron ZGW standaard: https://vng-realisatie.github.io/gemma-zaken/standaard/

// Bron Alg tools VNG: https://github.com/VNG-Realisatie/vng-api-common

namespace Roxit.ZGW.Zaken.Web.Controllers.v1._2;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_2)]
[ZgwApiVersion(Api.LatestVersion_1_5)]
[Consumes("application/json")]
[Produces("application/json")]
public class ZakenController : ZGWControllerBase
{
    private readonly IValidatorService _validatorService;

    public ZakenController(
        ILogger<ZakenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _validatorService = validatorService;
    }

    /// <summary>
    /// Werk een ZAAKEIGENSCHAP in zijn geheel bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(Contracts.v1._2.ApiRoutes.ZaakEigenschappen.Update, Name = Contracts.v1._2.Operations.ZaakEigenschappen.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakEigenschapResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakEigenschapRequestDto request, Guid zaak_uuid, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {zaak_uuid}, {uuid}", nameof(UpdateAsync), request, zaak_uuid, uuid);

        ZaakEigenschap zaakEigenschap = _mapper.Map<ZaakEigenschap>(request);

        var result = await _mediator.Send(
            new UpdateZaakEigenschapCommand
            {
                ZaakEigenschap = zaakEigenschap,
                ZaakId = zaak_uuid,
                Id = uuid,
            }
        );

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakEigenschapResponseDto>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// Werk een ZAAKEIGENSCHAP deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(Contracts.v1._2.ApiRoutes.ZaakEigenschappen.Update, Name = Contracts.v1._2.Operations.ZaakEigenschappen.PartialUpdate)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakEigenschapResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakEigenschapRequest, Guid zaak_uuid, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {zaak_uuid}, {uuid}", nameof(PartialUpdateAsync), zaak_uuid, uuid);

        var resultGet = await _mediator.Send(new GetZaakEigenschapQuery { Zaak = zaak_uuid, Eigenschap = uuid });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        ZaakEigenschapRequestDto mergedZaakEigenschapRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            ZaakEigenschapRequestDto,
            ZaakEigenschap
        >(resultGet.Result, partialZaakEigenschapRequest);

        if (!_validatorService.IsValid(mergedZaakEigenschapRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakEigenschap mergedZaakEigenschap = _mapper.Map<ZaakEigenschap>(mergedZaakEigenschapRequest);

        var resultUpd = await _mediator.Send(
            new UpdateZaakEigenschapCommand
            {
                ZaakEigenschap = mergedZaakEigenschap,
                ZaakId = zaak_uuid,
                Id = uuid,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        if (resultUpd.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakEigenschapResponse = _mapper.Map<ZaakEigenschapResponseDto>(resultUpd.Result);

        return Ok(zaakEigenschapResponse);
    }

    /// <summary>
    /// Verwijder een ZAAKEIGENSCHAP.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(Contracts.v1._2.ApiRoutes.ZaakEigenschappen.Delete, Name = Contracts.v1._2.Operations.ZaakEigenschappen.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid zaak_uuid, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {zaak_uuid}, {uuid}", nameof(DeleteAsync), zaak_uuid, uuid);

        var result = await _mediator.Send(new DeleteZaakEigenschapCommand { ZaakId = zaak_uuid, Id = uuid });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        return NoContent();
    }
}
