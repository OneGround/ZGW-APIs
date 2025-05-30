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
using Roxit.ZGW.Catalogi.Contracts.v1.Queries;
using Roxit.ZGW.Catalogi.Contracts.v1.Requests;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Authorization;
using Roxit.ZGW.Catalogi.Web.Configuration;
using Roxit.ZGW.Catalogi.Web.Contracts.v1;
using Roxit.ZGW.Catalogi.Web.Handlers.v1;
using Roxit.ZGW.Catalogi.Web.Models.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Catalogi.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class ResultaatTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ResultaatTypeController(
        ILogger<ResultaatTypeController> logger,
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
    /// Een specifieke RESULTAATTYPE opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ResultaatTypen.Get, Name = Operations.ResultaatTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ResultaatTypeResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetResultaatTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<ResultaatTypeResponseDto>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// Maak een RESULTAATTYPE aan.
    /// </summary>
    /// <remarks>
    /// Maak een RESULTAATTYPE aan. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ResultaatTypen.Create, Name = Operations.ResultaatTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ResultaatTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ResultaatTypeRequestDto resultTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), resultTypeRequest);

        ResultaatType resultType = _mapper.Map<ResultaatType>(resultTypeRequest);

        var result = await _mediator.Send(new CreateResultaatTypeCommand { ResultaatType = resultType, ZaakType = resultTypeRequest.ZaakType });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var resultTypeResponse = _mapper.Map<ResultaatTypeResponseDto>(result.Result);

        return Created(resultTypeResponse.Url, resultTypeResponse);
    }

    /// <summary>
    /// Alle RESULTAATTYPEn opvragen.
    /// </summary>
    /// <remarks>
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ResultaatTypen.GetAll, Name = Operations.ResultaatTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ResultaatTypeResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllResultaatTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ResultaatTypenPageSize));
        var filter = _mapper.Map<GetAllResultaatTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllResultaatTypenQuery { GetAllResultaatTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var resultTypenResponse = _mapper.Map<List<ResultaatTypeResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, resultTypenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Werk een RESULTAATTYPE in zijn geheel bij.
    /// </summary>
    /// <remarks>
    /// Werk een RESULTAATTYPE in zijn geheel bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ResultaatTypen.Update, Name = Operations.ResultaatTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ResultaatTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ResultaatTypeRequestDto resultTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), resultTypeRequest, id);

        ResultaatType resultType = _mapper.Map<ResultaatType>(resultTypeRequest);

        var result = await _mediator.Send(
            new UpdateResultaatTypeCommand
            {
                ResultaatType = resultType,
                Id = id,
                ZaakType = resultTypeRequest.ZaakType,
                IsPartialUpdate = false,
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

        var resultTypeResponse = _mapper.Map<ResultaatTypeResponseDto>(result.Result);

        return Ok(resultTypeResponse);
    }

    /// <summary>
    /// Werk een RESULTAATTYPE deels bij.
    /// </summary>
    /// <remarks>
    /// Werk een RESULTAATTYPE deels bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ResultaatTypen.Update, Name = Operations.ResultaatTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ResultaatTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialResultTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetResultaatTypeQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        ResultaatTypeRequestDto mergedResultTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<ResultaatTypeRequestDto, ResultaatType>(
            resultGet.Result,
            partialResultTypeRequest
        );

        if (!_validatorService.IsValid(mergedResultTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ResultaatType mergedResultType = _mapper.Map<ResultaatType>(mergedResultTypeRequest);

        var resultUpd = await _mediator.Send(
            new UpdateResultaatTypeCommand
            {
                ResultaatType = mergedResultType,
                Id = id,
                ZaakType = mergedResultTypeRequest.ZaakType,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var response = _mapper.Map<ResultaatTypeResponseDto>(resultUpd.Result);

        return Ok(response);
    }

    /// <summary>
    /// Verwijder een RESULTAATTYPE.
    /// </summary>
    /// <remarks>
    /// Verwijder een RESULTAATTYPE. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ResultaatTypen.Delete, Name = Operations.ResultaatTypen.Delete)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedDelete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteResultaatTypeCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        return NoContent();
    }
}
