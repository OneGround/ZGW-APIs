using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._2.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._2;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

// TODO: Reference the v1.2 DTO's later (not yet implemented)

namespace OneGround.ZGW.Catalogi.Web.Controllers.v1._2;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_2)]
public class InformatieObjectTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public InformatieObjectTypeController(
        ILogger<InformatieObjectTypeController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Get all INFORMATIEOBJECTTYPEN.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(Contracts.v1.ApiRoutes.InformatieObjectTypen.GetAll, Name = Contracts.v1.Operations.InformatieObjectTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<InformatieObjectTypeResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllInformatieObjectTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.InformatieObjectTypenPageSize));
        var filter = _mapper.Map<GetAllInformatieObjectTypenFilter>(queryParameters);

        var result = await _mediator.Send(
            new GetAllInformatieObjectTypenQuery() { GetAllInformatieObjectTypenFilter = filter, Pagination = pagination }
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var informatieObjectTypenResponse = _mapper.Map<List<InformatieObjectTypeResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            queryParameters,
            pagination,
            informatieObjectTypenResponse,
            result.Result.Count
        );

        return Ok(paginationResponse);
    }
}
