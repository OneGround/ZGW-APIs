using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Common.Web.Filters;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Documenten.Contracts.v1._5.Queries;
using Roxit.ZGW.Documenten.Contracts.v1._5.Responses;
using Roxit.ZGW.Documenten.Contracts.v1.Queries;
using Roxit.ZGW.Documenten.Contracts.v1.Responses;
using Roxit.ZGW.Documenten.Web.Authorization;
using Roxit.ZGW.Documenten.Web.Expands.v1._5;
using Roxit.ZGW.Documenten.Web.Handlers.v1;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Documenten.Web.Controllers.v1._5;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_5)]
[Consumes("application/json")]
[Produces("application/json")]
public class ObjectInformatieObjectenController : ZGWControllerBase
{
    private readonly IObjectExpander<InformatieObjectContext> _expander;

    public ObjectInformatieObjectenController(
        ILogger<ObjectInformatieObjectenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder,
        IExpanderFactory expanderFactory
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _expander = expanderFactory.Create<InformatieObjectContext>("informatieobject");
    }

    /// <summary>
    /// Alle OBJECT-INFORMATIEOBJECT relaties opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(Contracts.v1.ApiRoutes.ObjectInformatieObjecten.GetAll, Name = Contracts.v1.Operations.ObjectInformatieObjecten.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<ObjectInformatieObjectResponseExpandedDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [Expand]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllObjectInformatieObjectenQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var filter = _mapper.Map<Models.v1.GetAllObjectInformatieObjectenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllObjectInformatieObjectenQuery { GetAllObjectInformatieObjectenFilter = filter });

        var objectInformatieObjectenResponse = _mapper.Map<List<ObjectInformatieObjectResponseDto>>(result.Result);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var objectInformatieObjectenResponseWithOptionalExpand = objectInformatieObjectenResponse
            .Select(g =>
                _expander.ResolveAsync(expandLookup, new InformatieObjectContext { InformatieObject = g.InformatieObject, ObjectDto = g }).Result
            )
            .ToList();

        // TODO: Still deciding if this makes sense (because can generate lot of audittrail logs)
        //await _mediator.Send(new LogAuditTrailGetObjectListCommand
        //{
        //    RetrieveCatagory = RetrieveCatagory.All,
        //    TotalCount = result.Result.Count,
        //    AuditTrailOptions = new AuditTrailOptions { Bron = "DRC", Resource = "objectinformatieobject" }
        //});

        return Ok(objectInformatieObjectenResponseWithOptionalExpand);
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
    [HttpGet(Contracts.v1.ApiRoutes.ObjectInformatieObjecten.Get, Name = Contracts.v1.Operations.ObjectInformatieObjecten.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ObjectInformatieObjectResponseExpandedDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetObjectInformatieObjectQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetObjectInformatieObjectQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var objectInformatieObject = _mapper.Map<ObjectInformatieObjectResponseDto>(result.Result);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var objectInformatieObjectWithOptionalExpand = await _expander.ResolveAsync(
            expandLookup,
            new InformatieObjectContext { InformatieObject = objectInformatieObject.InformatieObject, ObjectDto = objectInformatieObject }
        );

        // TODO: Still deciding if this makes sense (because can generate lot of audittrail logs)
        //await _mediator.Send(new LogAuditTrailGetObjectCommand
        //{
        //    RetrieveCatagory = RetrieveCatagory.All,
        //    BaseEntity = result.Result.InformatieObject,
        //    SubEntity = result.Result,
        //    AuditTrailOptions = new AuditTrailOptions { Bron = "DRC", Resource = "objectinformatieobject" }
        //});

        return Ok(objectInformatieObjectWithOptionalExpand);
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
    [HttpHead(Contracts.v1.ApiRoutes.ObjectInformatieObjecten.Get, Name = Contracts.v1.Operations.ObjectInformatieObjecten.ReadHead)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetObjectInformatieObjectQueryParameters queryParameters)
    {
        return GetAsync(id, queryParameters);
    }
}
