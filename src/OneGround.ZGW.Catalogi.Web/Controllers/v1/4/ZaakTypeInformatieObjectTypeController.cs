using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Catalogi.Web.Contracts.v1._3;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Catalogi.Web.Models.v1._3;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Catalogi.Web.Controllers.v1._4;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_4)]
public class ZaakTypeInformatieObjectTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<ZaakTypeInformatieObjectTypeResponseDto> _expandValidator;
    private readonly ExpandEngine<ZaakTypeInformatieObjectTypeResponseDto> _expandEngine;

    public ZaakTypeInformatieObjectTypeController(
        ILogger<ZaakTypeInformatieObjectTypeController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IConfiguration configuration,
        IErrorResponseBuilder errorResponseBuilder,
        IPaginationHelper paginationHelper,
        ExpandValidator<ZaakTypeInformatieObjectTypeResponseDto> expandValidator,
        ExpandEngine<ZaakTypeInformatieObjectTypeResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Een specifieke ZAAKTYPE-INFORMATIEOBJECTTYPE opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakTypeInformatieObjectTypen.Get, Name = Operations.ZaakTypeInformatieObjectTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeInformatieObjectTypeResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakTypeInformatieObjectTypeQueryParameters>))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetZaakTypeInformatieObjectTypeQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var result = await _mediator.Send(new GetZaakTypeInformatieObjectTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<ZaakTypeInformatieObjectTypeResponseDto>(result.Result);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(response, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        return Ok(response);
    }

    /// <summary>
    /// Alle ZAAKTYPE-INFORMATIEOBJECTTYPEn opvragen.
    /// </summary>
    /// <remarks>
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakTypeInformatieObjectTypen.GetAll, Name = Operations.ZaakTypeInformatieObjectTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakTypeInformatieObjectTypeResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZaakTypeInformatieObjectTypenQueryParameters>))]
    [Expand]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakTypeInformatieObjectTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakTypeInformatieObjectTypenPageSize));
        var filter = _mapper.Map<GetAllZaakTypeInformatieObjectTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakTypeInformatieObjectTypenQuery { GetAllZaakTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaakTypeInformatieObjectTypenResponse = _mapper.Map<List<ZaakTypeInformatieObjectTypeResponseDto>>(result.Result.PageResult);

        // Handle optional expands on the returned DTOs. This is done after the mapping to the DTOs, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(zaakTypeInformatieObjectTypenResponse, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            queryParameters,
            pagination,
            zaakTypeInformatieObjectTypenResponse,
            result.Result.Count
        );

        return Ok(paginationResponse);
    }
}
