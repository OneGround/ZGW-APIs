using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Configuration;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Documenten.Web.Controllers.v1._7;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_7)]
[Consumes("application/json")]
[Produces("application/json")]
public class GebruiksRechtenController : ZGWControllerBase
{
    private readonly ExpandValidator<GebruiksRechtResponseDto> _expandValidator;
    private readonly ExpandEngine<GebruiksRechtResponseDto> _expandEngine;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public GebruiksRechtenController(
        ILogger<GebruiksRechtenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IErrorResponseBuilder errorResponseBuilder,
        IConfiguration configuration,
        ExpandValidator<GebruiksRechtResponseDto> expandValidator,
        ExpandEngine<GebruiksRechtResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;

        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Alle GEBRUIKSRECHTen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1.ApiRoutes.GebruiksRechten.GetAll, Name = Contracts.v1.Operations.GebruiksRechten.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<GebruiksRechtResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllGebruiksRechtenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] GetAllGebruiksRechtenQueryParameters queryParameters,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var (expandPaths, expandError) = _expandValidator.ParseAndValidate(queryParameters.Expand);
        if (expandError is not null)
        {
            return _errorResponseBuilder.BadRequest(
                new[] { new ValidationError("expand", ErrorCode.Invalid, expandError) },
                title: "Ongeldige expand parameter"
            );
        }
        if (!IsExpandEnabled(_applicationConfiguration.ExpandSettings.List, expandPaths))
        {
            return _errorResponseBuilder.BadRequest(
                new[] { new ValidationError("expand", ErrorCode.DisabledExpand, "Expand is uitgeschakeld op deze operatie.") },
                title: "Invalid input"
            );
        }

        var filter = _mapper.Map<Models.v1.GetAllGebruiksRechtenFilter>(queryParameters);

        var result = await _mediator.Send(new Handlers.v1.GetAllGebruiksRechtenQuery { GetAllGebruiksRechtenFilter = filter }, cancellationToken);

        var gebruiksRechtenResponse = _mapper.Map<List<GebruiksRechtResponseDto>>(result.Result);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(gebruiksRechtenResponse, expandPaths);
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
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                TotalCount = result.Result.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" },
            },
            cancellationToken
        );

        return Ok(gebruiksRechtenResponse);
    }

    /// <summary>
    /// Een specifieke GEBRUIKSRECHT opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1.ApiRoutes.GebruiksRechten.Get, Name = Contracts.v1.Operations.GebruiksRechten.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(GebruiksRechtResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ETagFilter]
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetGebruiksRechtQueryParameters>))]
    public async Task<IActionResult> GetAsync(
        Guid id,
        [FromQuery] GetGebruiksRechtQueryParameters queryParameters,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var (expandPaths, expandError) = _expandValidator.ParseAndValidate(queryParameters.Expand);
        if (expandError is not null)
        {
            return _errorResponseBuilder.BadRequest(
                new[] { new ValidationError("expand", ErrorCode.Invalid, expandError) },
                title: "Ongeldige expand parameter"
            );
        }
        if (!IsExpandEnabled(_applicationConfiguration.ExpandSettings.Get, expandPaths))
        {
            return _errorResponseBuilder.BadRequest(
                new[] { new ValidationError("expand", ErrorCode.DisabledExpand, "Expand is uitgeschakeld op deze operatie.") },
                title: "Invalid input"
            );
        }

        var result = await _mediator.Send(new Handlers.v1.GetGebruiksRechtQuery { Id = id }, cancellationToken);

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var gebruiksrecht = _mapper.Map<GebruiksRechtResponseDto>(result.Result);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(gebruiksrecht, expandPaths);
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
                BaseEntity = result.Result.InformatieObject,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" },
                LegacyAuditTrail = result.Result.InformatieObject.LegacyAuditTrail,
            },
            cancellationToken
        );

        return Ok(gebruiksrecht);
    }

    /// <summary>
    /// De headers voor een specifiek(e) GEBRUIKSRECHT INFORMATIEOBJECT opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpHead(Contracts.v1.ApiRoutes.GebruiksRechten.Get, Name = Contracts.v1.Operations.GebruiksRechten.ReadHead)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [ETagFilter]
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetGebruiksRechtQueryParameters>))]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetGebruiksRechtQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        return GetAsync(id, queryParameters, cancellationToken);
    }
}
