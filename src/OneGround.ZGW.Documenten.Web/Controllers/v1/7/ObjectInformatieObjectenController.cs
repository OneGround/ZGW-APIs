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
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Services;
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
public class ObjectInformatieObjectenController : ZGWControllerBase
{
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<ObjectInformatieObjectResponseDto> _expandValidator;
    private readonly ExpandEngine<ObjectInformatieObjectResponseDto> _expandEngine;
    private readonly MapsterMapper.IMapper _mapsterMapper;

    public ObjectInformatieObjectenController(
        ILogger<ObjectInformatieObjectenController> logger,
        IMediator mediator,
        AutoMapper.IMapper mapper,
        MapsterMapper.IMapper mapsterMapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IErrorResponseBuilder errorResponseBuilder,
        ExpandValidator<ObjectInformatieObjectResponseDto> expandValidator,
        ExpandEngine<ObjectInformatieObjectResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _mapsterMapper = mapsterMapper;
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;

        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Alle OBJECT-INFORMATIEOBJECT relaties opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1.ApiRoutes.ObjectInformatieObjecten.GetAll, Name = Contracts.v1.Operations.ObjectInformatieObjecten.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<ObjectInformatieObjectResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllObjectInformatieObjectenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] GetAllObjectInformatieObjectenQueryParameters queryParameters,
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
        if (!IsExpandEnabled(_applicationConfiguration.ExpandSettings.Get, expandPaths))
        {
            return _errorResponseBuilder.BadRequest(
                new[] { new ValidationError("expand", ErrorCode.DisabledExpand, "Expand is uitgeschakeld op deze operatie.") },
                title: "Invalid input"
            );
        }
        if (!IsExpandEnabled(_applicationConfiguration.ExpandSettings.Get, expandPaths))
        {
            return _errorResponseBuilder.BadRequest(
                new[] { new ValidationError("expand", ErrorCode.DisabledExpand, "Expand is uitgeschakeld op deze operatie.") },
                title: "Invalid input"
            );
        }

        var filter = _mapsterMapper.Map<Models.v1.GetAllObjectInformatieObjectenFilter>(queryParameters);

        var result = await _mediator.Send(
            new Handlers.v1.GetAllObjectInformatieObjectenQuery { GetAllObjectInformatieObjectenFilter = filter },
            cancellationToken
        );

        var objectInformatieObjectenResponse = _mapsterMapper.Map<List<ObjectInformatieObjectResponseDto>>(result.Result);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(objectInformatieObjectenResponse, expandPaths);
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

        return Ok(objectInformatieObjectenResponse);
    }

    /// <summary>
    /// Een specifieke OBJECT-INFORMATIEOBJECT relatie opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1.ApiRoutes.ObjectInformatieObjecten.Get, Name = Contracts.v1.Operations.ObjectInformatieObjecten.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ObjectInformatieObjectResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ETagFilter]
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetObjectInformatieObjectQueryParameters>))]
    public async Task<IActionResult> GetAsync(
        Guid id,
        [FromQuery] GetObjectInformatieObjectQueryParameters queryParameters,
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

        var result = await _mediator.Send(new Handlers.v1.GetObjectInformatieObjectQuery { Id = id }, cancellationToken);

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var objectInformatieObject = _mapsterMapper.Map<ObjectInformatieObjectResponseDto>(result.Result);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(objectInformatieObject, expandPaths);
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

        // TODO: Still deciding if this makes sense (because can generate lot of audittrail logs)
        //await _mediator.Send(new LogAuditTrailGetObjectCommand
        //{
        //    RetrieveCatagory = RetrieveCatagory.All,
        //    BaseEntity = result.Result.InformatieObject,
        //    SubEntity = result.Result,
        //    AuditTrailOptions = new AuditTrailOptions { Bron = "DRC", Resource = "objectinformatieobject" }
        //});

        return Ok(objectInformatieObject);
    }

    /// <summary>
    /// De headers van een specifieke OBJECT-INFORMATIEOBJECT relatie opvragen.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpHead(Contracts.v1.ApiRoutes.ObjectInformatieObjecten.Get, Name = Contracts.v1.Operations.ObjectInformatieObjecten.ReadHead)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [ETagFilter]
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetObjectInformatieObjectQueryParameters>))]
    public Task<IActionResult> HeadAsync(
        Guid id,
        [FromQuery] GetObjectInformatieObjectQueryParameters queryParameters,
        CancellationToken cancellationToken
    )
    {
        return GetAsync(id, queryParameters, cancellationToken);
    }
}
