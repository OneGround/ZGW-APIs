using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Models.v1;
using Swashbuckle.AspNetCore.Annotations;
// The eight using-ALIASES below decide which mapping pairs this controller resolves, and they are easy to
// miss: the file also imports Contracts.v1._5.Requests.ZaakObject above, but an alias beats a using-directive
// for a simple name. So every unqualified XxxZaakObjectRequestDto in this file is the v1 type, and the eight
// per-subtype merges run on the v1 pairs in MappingProfiles/v1/DomainToResponseRegister.cs - only the
// unaliased base merge uses a v1.5 type. Changing or dropping an alias silently moves a merge to a different
// registered pair. Copied verbatim from Controllers/v1/5/ZaakObjectenController.cs -- AddAsync/UpdateAsync/
// PartialUpdateAsync/DeleteAsync are unchanged for 1.7 (see class remarks below).
using AdresZaakObjectRequestDto = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.AdresZaakObjectRequestDto;
using BuurtZaakObjectRequestDto = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.BuurtZaakObjectRequestDto;
using GemeenteZaakObjectRequestDto = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.GemeenteZaakObjectRequestDto;
using KadastraleOnroerendeZaakObjectRequestDto = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.KadastraleOnroerendeZaakObjectRequestDto;
using OverigeZaakObjectRequestDto = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.OverigeZaakObjectRequestDto;
using PandZaakObjectRequestDto = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.PandZaakObjectRequestDto;
using TerreinGebouwdObjectZaakObjectRequestDto = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.TerreinGebouwdObjectZaakObjectRequestDto;
using WozWaardeZaakObjectRequestDto = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.WozWaardeZaakObjectRequestDto;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1._7;

// Note: Only GetAllAsync/GetAsync/HeadAsync use the new (v1.7) ExpandEngine and the new
// v1._7.Responses.ZaakObject.ZaakObjectResponseDto (which implements IExpandable). Add/Update/
// PartialUpdate/Delete's payload does not change for 1.7, so they keep using the v1._5 Request/
// Response DTOs and the v1._5 MediatR commands they already send -- same reasoning as
// Controllers/v1/7/ZaakRollenController.cs, except here ALL actions (including GetAll/Get/Head) already
// used the v1._5 handlers even before 1.7, so there is no v1/v1._5 Handlers split to worry about --
// one blanket "using Handlers.v1._5" covers every action in this file. There is no "fields" mechanism
// here -- unlike Zaken's /_zoek, the ZRC 1.7.0 spec only adds "expand" for zaakobjecten.
[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_7)]
public class ZaakObjectenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly Validators.v1._5.ZaakObject.IZaakObjectValidatorService _zaakObjectValidatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly IRequestMerger _requestMerger;
    private readonly ExpandValidator<ZaakObjectResponseDto> _expandValidator;
    private readonly ExpandEngine<ZaakObjectResponseDto> _expandEngine;

    public ZaakObjectenController(
        ILogger<ZaakObjectenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        Validators.v1._5.ZaakObject.IZaakObjectValidatorService zaakObjectValidatorService,
        IErrorResponseBuilder errorResponseBuilder,
        ExpandValidator<ZaakObjectResponseDto> expandValidator,
        ExpandEngine<ZaakObjectResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _requestMerger = requestMerger;
        _paginationHelper = paginationHelper;
        _zaakObjectValidatorService = zaakObjectValidatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;
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
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.ZaakObjecten.GetAll, Name = Contracts.v1._5.Operations.ZaakObjecten.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakObjectResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZaakObjectenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakObjectenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var expandValidationResult = ValidateExpand(
            _expandValidator,
            queryParameters.Expand,
            _applicationConfiguration.ExpandSettings.List,
            out var expandPaths
        );
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakObjectenPageSize));
        var filter = _mapper.Map<GetAllZaakObjectenFilter>(queryParameters);

        var result = await _mediator.Send(new Handlers.v1._5.GetAllZaakObjectenQuery { GetAllZaakObjectenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var response = _mapper.Map<List<ZaakObjectResponseDto>>(result.Result.PageResult);

        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(response, expandPaths);
            }
            catch (ExpandExternalServiceException ex)
            {
                return ExterneServiceFout(ex.ServiceName, ex.ServiceUrl);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, response, result.Result.Count);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakobject" },
            }
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifiek ZAAKOBJECT opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.ZaakObjecten.Get, Name = Contracts.v1._5.Operations.ZaakObjecten.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakObjectQueryParameters>))]
    [ETagFilter]
    public async Task<IActionResult> GetAsync([FromQuery] GetZaakObjectQueryParameters queryParameters, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var expandValidationResult = ValidateExpand(
            _expandValidator,
            queryParameters.Expand,
            _applicationConfiguration.ExpandSettings.Get,
            out var expandPaths
        );
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakObjectQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakObjectResponseDto>(result.Result);

        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(response, expandPaths);
            }
            catch (ExpandExternalServiceException ex)
            {
                return ExterneServiceFout(ex.ServiceName, ex.ServiceUrl);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakobject" },
                LegacyAuditTrail = result.Result.Zaak.LegacyAuditTrail,
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// De headers voor een specifiek ZAAKOBJECT opvragen.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(Contracts.v1._5.ApiRoutes.ZaakObjecten.Get, Name = Contracts.v1._5.Operations.ZaakObjecten.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakObjectQueryParameters>))]
    [ETagFilter]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetZaakObjectQueryParameters queryParameters)
    {
        return GetAsync(queryParameters, id);
    }

    /// <summary>
    /// Maak een ZAAKOBJECT aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(Contracts.v1._5.ApiRoutes.ZaakObjecten.Create, Name = Contracts.v1._5.Operations.ZaakObjecten.Create)]
    [Scope(AuthorizationScopes.Zaken.Create, AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(Zaken.Contracts.v1._5.Responses.ZaakObject.ZaakObjectResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] Zaken.Contracts.v1._5.Requests.ZaakObject.ZaakObjectRequestDto zaakObjectRequest) // Note: zaakObjectRequest can be of types: ZaakObjectRequestDto, RelatieZaakObjectRequestDto or InvalidZaakObjectRequestDto (due ZaakObjectRequestDtoJsonConverter)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakObjectRequest);

        if (!_zaakObjectValidatorService.Validate(zaakObjectRequest, out var validationResult)) // Note: Extends validator with v1.2 specific
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakObject zaakObject = _mapper.Map<ZaakObject>(zaakObjectRequest);

        var result = await _mediator.Send(new Handlers.v1._5.CreateZaakObjectCommand { ZaakObject = zaakObject, ZaakUrl = zaakObjectRequest.Zaak });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<Zaken.Contracts.v1._5.Responses.ZaakObject.ZaakObjectResponseDto>(result.Result);

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
    [HttpPut(Contracts.v1._5.ApiRoutes.ZaakObjecten.Update, Name = Contracts.v1._5.Operations.ZaakObjecten.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Zaken.Contracts.v1._5.Responses.ZaakObject.ZaakObjectResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] Zaken.Contracts.v1._5.Requests.ZaakObject.ZaakObjectRequestDto zaakobjectRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), zaakobjectRequest, id);

        // Note: For v1.2 we have a new ObjectTypeOverigeDefinitie to be validated against the same datacontract ZaakObjectRequestDto
        if (!_zaakObjectValidatorService.Validate(zaakobjectRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        var zaakobject = _mapper.Map<ZaakObject>(zaakobjectRequest);

        var result = await _mediator.Send(new Handlers.v1._5.UpdateZaakObjectCommand { ZaakObject = zaakobject, ZaakObjectId = id });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakObjectResponse = _mapper.Map<Zaken.Contracts.v1._5.Responses.ZaakObject.ZaakObjectResponseDto>(result.Result);

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
    [HttpPatch(Contracts.v1._5.ApiRoutes.ZaakObjecten.Update, Name = Contracts.v1._5.Operations.ZaakObjecten.PartialUpdate)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Zaken.Contracts.v1._5.Responses.ZaakObject.ZaakObjectResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakObjectRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new Handlers.v1._5.GetZaakObjectQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        Zaken.Contracts.v1._5.Requests.ZaakObject.ZaakObjectRequestDto mergedZaakObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            Zaken.Contracts.v1._5.Requests.ZaakObject.ZaakObjectRequestDto,
            ZaakObject
        >(resultGet.Result, partialZaakObjectRequest);

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
            new Handlers.v1._5.UpdateZaakObjectCommand
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

        var zaakObjectResponse = _mapper.Map<Zaken.Contracts.v1._5.Responses.ZaakObject.ZaakObjectResponseDto>(resultUpd.Result);

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
    [HttpDelete(Contracts.v1._5.ApiRoutes.ZaakObjecten.Delete, Name = Contracts.v1._5.Operations.ZaakObjecten.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate, AuthorizationScopes.Zaken.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new Handlers.v1._5.DeleteZaakObjectCommand { ZaakObjectId = id });

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
