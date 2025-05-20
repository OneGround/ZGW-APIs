using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Contracts.v1;
using OneGround.ZGW.Zaken.Web.Handlers.v1;
using OneGround.ZGW.Zaken.Web.Models.v1;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
public class ZaakResultatenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZaakResultatenController(
        ILogger<ZaakResultatenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Alle RESULTAATen van ZAAKen opvragen.
    /// </summary>
    /// <remarks>Deze lijst kan gefilterd wordt met query-string parameters.</remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakResultaten.GetAll, Name = Operations.ZaakResultaten.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakResultaatResponseDto>))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakResultatenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakResultatenPageSize));
        var filter = _mapper.Map<GetAllZaakResultatenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakResultatenQuery { GetAllZaakResultatenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var response = _mapper.Map<List<ZaakResultaatResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, response, result.Result.Count);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "resultaat" },
            }
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifiek RESULTAAT opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakResultaten.Get, Name = Operations.ZaakResultaten.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResultaatResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakResultaatQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakResultaatResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "resultaat" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Maak een RESULTAAT bij een ZAAK aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakResultaten.Create, Name = Operations.ZaakResultaten.Create)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakResultaatResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> AddAsync([FromBody] ZaakResultaatRequestDto request)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), request);

        ZaakResultaat zaakResultaat = _mapper.Map<ZaakResultaat>(request);

        var result = await _mediator.Send(new CreateZaakResultaatCommand { ZaakResultaat = zaakResultaat, ZaakUrl = request.Zaak });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakResultaatResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Werk een RESULTAAT in zijn geheel bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ZaakResultaten.Update, Name = Operations.ZaakResultaten.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResultaatResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakResultaatRequestDto request, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), request, id);

        ZaakResultaat zaakResultaat = _mapper.Map<ZaakResultaat>(request);

        var result = await _mediator.Send(
            new UpdateZaakResultaatCommand
            {
                ZaakResultaat = zaakResultaat,
                Id = id,
                ZaakUrl = request.Zaak,
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

        var response = _mapper.Map<ZaakResultaatResponseDto>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// Werk een RESULTAAT deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ZaakResultaten.Update, Name = Operations.ZaakResultaten.PartialUpdate)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResultaatResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakResultaatRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetZaakResultaatQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        ZaakResultaatRequestDto mergedZaakResultaatRequest = _requestMerger.MergePartialUpdateToObjectRequest<ZaakResultaatRequestDto, ZaakResultaat>(
            resultGet.Result,
            partialZaakResultaatRequest
        );

        if (!_validatorService.IsValid(mergedZaakResultaatRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakResultaat mergedZaakResultaat = _mapper.Map<ZaakResultaat>(mergedZaakResultaatRequest);

        var resultUpd = await _mediator.Send(
            new UpdateZaakResultaatCommand
            {
                ZaakResultaat = mergedZaakResultaat,
                Id = id,
                ZaakUrl = mergedZaakResultaatRequest.Zaak,
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

        var zaakResultaatResponse = _mapper.Map<ZaakResultaatResponseDto>(resultUpd.Result);

        return Ok(zaakResultaatResponse);
    }

    /// <summary>
    /// Verwijder een RESULTAAT van een ZAAK.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakResultaten.Delete, Name = Operations.ZaakResultaten.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteZaakResultaatCommand { Id = id });

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
