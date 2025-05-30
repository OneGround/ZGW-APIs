using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Roxit.ZGW.Autorisaties.Contracts.v1.Requests;
using Roxit.ZGW.Autorisaties.Contracts.v1.Responses;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Autorisaties.Web.Authorization;
using Roxit.ZGW.Autorisaties.Web.Configuration;
using Roxit.ZGW.Autorisaties.Web.Contracts;
using Roxit.ZGW.Autorisaties.Web.Contracts.v1.Requests.Queries;
using Roxit.ZGW.Autorisaties.Web.Handlers;
using Roxit.ZGW.Autorisaties.Web.Models;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Autorisaties.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class ApplicatiesController : ZGWControllerBase
{
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;

    public ApplicatiesController(
        ILogger<ApplicatiesController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder,
        IValidatorService validatorService
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Geef een collectie van applicaties, met ingesloten autorisaties.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Applicaties.GetAll, Name = Operations.Applicaties.List)]
    [Scope(AuthorizationScopes.Autorisaties.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ApplicatieResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllApplicatiesQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ApplicatiePageSize));
        var filter = _mapper.Map<GetAllApplicatiesFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllApplicatiesQuery() { GetAllApplicatiesFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var applicatiesResponse = _mapper.Map<List<ApplicatieResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, applicatiesResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Vraag een applicatie op, met ingesloten autorisaties.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Applicaties.Get, Name = Operations.Applicaties.Read)]
    [Scope(AuthorizationScopes.Autorisaties.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ApplicatieResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetApplicatieQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<ApplicatieResponseDto>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// Vraag een applicatie op, op basis van clientId.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Applicaties.GetByConsumer, Name = Operations.Applicaties.GetByConsumer)]
    [Scope(AuthorizationScopes.Autorisaties.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ApplicatieResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetByConsumerAsync(string clientId)
    {
        _logger.LogDebug("{ControllerMethod} called with {ClientId}", nameof(GetByConsumerAsync), clientId);

        if (string.IsNullOrEmpty(clientId)) //Can not be moved to handler logic, since this uses same one as GetById. Should we add new GetByConsumerApplicatieQueryHandler?
        {
            return BadRequest(
                new ErrorResponse
                {
                    Type = $"{BaseUrl}{ErrorCategory.ValidationError}",
                    Code = ErrorCode.Invalid,
                    Title = "clientId fout.",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = "clientId moet worden opgegeven.",
                }
            );
        }

        var result = await _mediator.Send(new GetApplicatieQuery { ClientId = clientId });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<ApplicatieResponseDto>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// Registreer een applicatie met een bepaalde set van autorisaties.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Applicaties.Create, Name = Operations.Applicaties.Create)]
    [Scope(AuthorizationScopes.Autorisaties.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ApplicatieResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ApplicatieRequestDto applicatieRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), applicatieRequest);

        Applicatie applicatie = _mapper.Map<Applicatie>(applicatieRequest);

        var result = await _mediator.Send(new CreateApplicatieCommand() { Applicatie = applicatie });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var applicatieResponse = _mapper.Map<ApplicatieResponseDto>(result.Result);

        return Created(result.Result.Url, applicatieResponse);
    }

    /// <summary>
    /// Werk de applicatie bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.Applicaties.Update, Name = Operations.Applicaties.Update)]
    [Scope(AuthorizationScopes.Autorisaties.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ApplicatieResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ApplicatieRequestDto request, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), request, id);

        Applicatie applicatie = _mapper.Map<Applicatie>(request);

        var result = await _mediator.Send(new UpdateApplicatieCommand() { Id = id, Applicatie = applicatie });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<ApplicatieResponseDto>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// Werk (een deel van) de applicatie bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.Applicaties.Update, Name = Operations.Applicaties.PartialUpdate)]
    [Scope(AuthorizationScopes.Autorisaties.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ApplicatieResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialApplicatieRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetApplicatieQuery() { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        ApplicatieRequestDto mergedApplicatieRequest = _requestMerger.MergePartialUpdateToObjectRequest<ApplicatieRequestDto, Applicatie>(
            resultGet.Result,
            partialApplicatieRequest
        );

        if (!_validatorService.IsValid(mergedApplicatieRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        Applicatie mergedApplicatie = _mapper.Map<Applicatie>(mergedApplicatieRequest);

        var resultUpd = await _mediator.Send(new UpdateApplicatieCommand() { Id = id, Applicatie = mergedApplicatie });

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var applicatieResponse = _mapper.Map<ApplicatieResponseDto>(resultUpd.Result);

        return Ok(applicatieResponse);
    }

    /// <summary>
    /// Verwijder een applicatie met de bijhorende autorisaties.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.Applicaties.Delete, Name = Operations.Applicaties.Delete)]
    [Scope(AuthorizationScopes.Autorisaties.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteApplicatieCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden(result.Errors);
        }

        return NoContent();
    }
}
