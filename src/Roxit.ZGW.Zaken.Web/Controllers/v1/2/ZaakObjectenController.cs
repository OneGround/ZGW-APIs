using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;
using Roxit.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.ZaakObject;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Configuration;
using Roxit.ZGW.Zaken.Web.Contracts.v1._2;
using Roxit.ZGW.Zaken.Web.Handlers.v1;
using Roxit.ZGW.Zaken.Web.Handlers.v1._2;
using Roxit.ZGW.Zaken.Web.Models.v1;
using Roxit.ZGW.Zaken.Web.Validators.v1.ZaakObject;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Zaken.Web.Controllers.v1._2;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_2)]
[Consumes("application/json")]
[Produces("application/json")]
public class ZaakObjectenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IZaakObjectValidatorService _zaakObjectValidatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZaakObjectenController(
        ILogger<ZaakObjectenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IZaakObjectValidatorService zaakObjectValidatorService,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _zaakObjectValidatorService = zaakObjectValidatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Alle ZAAKOBJECTen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakObjecten.GetAll, Name = Operations.ZaakObjecten.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakObjectResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakObjectenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakObjectenPageSize));
        var filter = _mapper.Map<GetAllZaakObjectenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakObjectenQuery { GetAllZaakObjectenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaakObjectResponse = _mapper.Map<IEnumerable<ZaakObject>, List<ZaakObjectResponseDto>>(
            result.Result.PageResult,
            opt =>
            {
                opt.AfterMap((_, dest) => dest.ForEach(o => o.Version = "1.2")); // Maps with additional (1.2) data
            }
        );

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, zaakObjectResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifiek ZAAKOBJECT opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakObjecten.Get, Name = Operations.ZaakObjecten.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakObjectQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakObject, ZaakObjectResponseDto>(
            result.Result,
            opt =>
            {
                opt.AfterMap((_, dest) => dest.Version = "1.2"); // Maps with additional (1.2) data
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Maak een ZAAKOBJECT aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakObjecten.Create, Name = Operations.ZaakObjecten.Create)]
    [Scope(AuthorizationScopes.Zaken.Create, AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakObjectResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakObjectRequestDto zaakObjectRequest) // Note: zaakObjectRequest can be of types: ZaakObjectRequestDto, RelatieZaakObjectRequestDto or InvalidZaakObjectRequestDto (due ZaakObjectRequestDtoJsonConverter)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakObjectRequest);

        if (!_zaakObjectValidatorService.Validate(zaakObjectRequest, out var validationResult)) // Note: Extends validator with v1.2 specific
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakObject zaakObject = _mapper.Map<ZaakObject>(zaakObjectRequest);

        var result = await _mediator.Send(new CreateZaakObjectCommand { ZaakObject = zaakObject, ZaakUrl = zaakObjectRequest.Zaak });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakObject, ZaakObjectResponseDto>(
            result.Result,
            opt =>
            {
                opt.AfterMap((_, dest) => dest.Version = "1.2"); // Maps with additional (1.2) data
            }
        );

        return Created(response.Url, response);
    }

    /// <summary>
    /// Werk een ZAAKOBJECT in zijn geheel bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ZaakObjecten.Update, Name = Operations.ZaakObjecten.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakObjectRequestDto zaakobjectRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), zaakobjectRequest, id);

        // Note: For v1.2 we have a new ObjectTypeOverigeDefinitie to be validated against the same datacontract ZaakObjectRequestDto
        if (!_zaakObjectValidatorService.Validate(zaakobjectRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        var zaakobject = _mapper.Map<ZaakObject>(zaakobjectRequest);

        var result = await _mediator.Send(new UpdateZaakObjectCommand { ZaakObject = zaakobject, ZaakObjectId = id });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakObjectResponse = _mapper.Map<ZaakObject, ZaakObjectResponseDto>(
            result.Result,
            opt =>
            {
                opt.AfterMap((_, dest) => dest.Version = "1.2"); // Maps with additional (1.2) data
            }
        );

        return Ok(zaakObjectResponse);
    }

    /// <summary>
    /// Werk een ZAAKOBJECT deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ZaakObjecten.Update, Name = Operations.ZaakObjecten.PartialUpdate)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakObjectRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetZaakObjectQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        ZaakObjectRequestDto mergedZaakObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<ZaakObjectRequestDto, ZaakObject>(
            resultGet.Result,
            partialZaakObjectRequest,
            opt =>
            {
                opt.AfterMap((_, dest) => dest.Version = "1.2"); // To be merged v1.2 correctly tell the mapper to include additional (1.2) data
            }
        );

        if (!_zaakObjectValidatorService.Validate(mergedZaakObjectRequest, out var validationResult, resultGet.Result))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakObject mergedZaakObject = _mapper.Map<ZaakObject>(mergedZaakObjectRequest);

        switch (resultGet.Result.ObjectType)
        {
            case ObjectType.adres:
                AdresZaakObjectRequestDto mergedAdresZaakObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
                    AdresZaakObjectRequestDto,
                    AdresZaakObject
                >(resultGet.Result.Adres, partialZaakObjectRequest);

                if (!_zaakObjectValidatorService.IsValidAdresZaakObject(mergedAdresZaakObjectRequest.ObjectIdentificatie, out validationResult))
                {
                    return _errorResponseBuilder.BadRequest(validationResult);
                }

                mergedZaakObject.Adres = _mapper.Map<AdresZaakObject>(mergedAdresZaakObjectRequest);
                break;

            case ObjectType.buurt:
                BuurtZaakObjectRequestDto mergedBuurtZaakObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
                    BuurtZaakObjectRequestDto,
                    BuurtZaakObject
                >(resultGet.Result.Buurt, partialZaakObjectRequest);

                if (!_zaakObjectValidatorService.IsValidBuurtZaakObject(mergedBuurtZaakObjectRequest.ObjectIdentificatie, out validationResult))
                {
                    return _errorResponseBuilder.BadRequest(validationResult);
                }
                mergedZaakObject.Buurt = _mapper.Map<BuurtZaakObject>(mergedBuurtZaakObjectRequest);
                break;

            case ObjectType.gemeente:
                GemeenteZaakObjectRequestDto mergedGemeenteZaakObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
                    GemeenteZaakObjectRequestDto,
                    GemeenteZaakObject
                >(resultGet.Result.Gemeente, partialZaakObjectRequest);

                if (!_zaakObjectValidatorService.IsValidGemeenteZaakObject(mergedGemeenteZaakObjectRequest.ObjectIdentificatie, out validationResult))
                {
                    return _errorResponseBuilder.BadRequest(validationResult);
                }
                mergedZaakObject.Gemeente = _mapper.Map<GemeenteZaakObject>(mergedGemeenteZaakObjectRequest);
                break;

            case ObjectType.kadastrale_onroerende_zaak:
                KadastraleOnroerendeZaakObjectRequestDto mergedKadastraleOnroerendeZaakObjectRequest =
                    _requestMerger.MergePartialUpdateToObjectRequest<KadastraleOnroerendeZaakObjectRequestDto, KadastraleOnroerendeZaakObject>(
                        resultGet.Result.KadastraleOnroerendeZaak,
                        partialZaakObjectRequest
                    );

                if (
                    !_zaakObjectValidatorService.IsValidKadastraleOnroerendeZaakObject(
                        mergedKadastraleOnroerendeZaakObjectRequest.ObjectIdentificatie,
                        out validationResult
                    )
                )
                {
                    return _errorResponseBuilder.BadRequest(validationResult);
                }
                mergedZaakObject.KadastraleOnroerendeZaak = _mapper.Map<KadastraleOnroerendeZaakObject>(mergedKadastraleOnroerendeZaakObjectRequest);
                break;

            case ObjectType.overige:
                OverigeZaakObjectRequestDto mergedOverigeZaakObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
                    OverigeZaakObjectRequestDto,
                    OverigeZaakObject
                >(resultGet.Result.Overige, partialZaakObjectRequest);

                if (!_zaakObjectValidatorService.IsValidOverigeZaakObject(mergedOverigeZaakObjectRequest.ObjectIdentificatie, out validationResult))
                {
                    return _errorResponseBuilder.BadRequest(validationResult);
                }
                mergedZaakObject.Overige = _mapper.Map<OverigeZaakObject>(mergedOverigeZaakObjectRequest);
                break;

            case ObjectType.pand:
                PandZaakObjectRequestDto mergedPandZaakObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
                    PandZaakObjectRequestDto,
                    PandZaakObject
                >(resultGet.Result.Pand, partialZaakObjectRequest);

                if (!_zaakObjectValidatorService.IsValidPandZaakObject(mergedPandZaakObjectRequest.ObjectIdentificatie, out validationResult))
                {
                    return _errorResponseBuilder.BadRequest(validationResult);
                }
                mergedZaakObject.Pand = _mapper.Map<PandZaakObject>(mergedPandZaakObjectRequest);
                break;

            case ObjectType.terrein_gebouwd_object:
                TerreinGebouwdObjectZaakObjectRequestDto mergedTerreinGebouwdObjectZaakObjectRequest =
                    _requestMerger.MergePartialUpdateToObjectRequest<TerreinGebouwdObjectZaakObjectRequestDto, TerreinGebouwdObjectZaakObject>(
                        resultGet.Result.TerreinGebouwdObject,
                        partialZaakObjectRequest
                    );

                if (
                    !_zaakObjectValidatorService.IsValidTerreinGebouwdObjectZaakObject(
                        mergedTerreinGebouwdObjectZaakObjectRequest.ObjectIdentificatie,
                        out validationResult
                    )
                )
                {
                    return _errorResponseBuilder.BadRequest(validationResult);
                }
                mergedZaakObject.TerreinGebouwdObject = _mapper.Map<TerreinGebouwdObjectZaakObject>(mergedTerreinGebouwdObjectZaakObjectRequest);
                break;

            case ObjectType.woz_waarde:
                WozWaardeZaakObjectRequestDto mergedWozWaardeZaakObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
                    WozWaardeZaakObjectRequestDto,
                    WozWaardeZaakObject
                >(resultGet.Result.WozWaardeObject, partialZaakObjectRequest);

                if (
                    !_zaakObjectValidatorService.IsValidWozWaardeZaakObject(
                        mergedWozWaardeZaakObjectRequest.ObjectIdentificatie,
                        out validationResult
                    )
                )
                {
                    return _errorResponseBuilder.BadRequest(validationResult);
                }
                mergedZaakObject.WozWaardeObject = _mapper.Map<WozWaardeZaakObject>(mergedWozWaardeZaakObjectRequest);
                break;
        }

        var resultUpd = await _mediator.Send(
            new UpdateZaakObjectCommand
            {
                ZaakObject = mergedZaakObject,
                ZaakObjectId = id,
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

        var zaakObjectResponse = _mapper.Map<ZaakObject, ZaakObjectResponseDto>(
            resultUpd.Result,
            opt =>
            {
                opt.AfterMap((_, dest) => dest.Version = "1.2"); // Maps with additional (1.2) data
            }
        );

        return Ok(zaakObjectResponse);
    }

    /// <summary>
    /// Verwijder een ZAAKOBJECT.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakObjecten.Delete, Name = Operations.ZaakObjecten.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate, AuthorizationScopes.Zaken.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteZaakObjectCommand { ZaakObjectId = id });

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

        return NoContent();
    }
}
