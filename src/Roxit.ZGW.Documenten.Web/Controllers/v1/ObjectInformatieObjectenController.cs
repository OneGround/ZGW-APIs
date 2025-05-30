using System;
using System.Collections.Generic;
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
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Documenten.Contracts.v1.Queries;
using Roxit.ZGW.Documenten.Contracts.v1.Requests;
using Roxit.ZGW.Documenten.Contracts.v1.Responses;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.Authorization;
using Roxit.ZGW.Documenten.Web.Contracts.v1;
using Roxit.ZGW.Documenten.Web.Handlers.v1;
using Roxit.ZGW.Documenten.Web.Models.v1;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Documenten.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
public class ObjectInformatieObjectenController : ZGWControllerBase
{
    public ObjectInformatieObjectenController(
        ILogger<ObjectInformatieObjectenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder) { }

    /// <summary>
    /// Alle OBJECT-INFORMATIEOBJECT relaties opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ObjectInformatieObjecten.GetAll, Name = Operations.ObjectInformatieObjecten.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<ObjectInformatieObjectResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllObjectInformatieObjectenQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var filter = _mapper.Map<GetAllObjectInformatieObjectenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllObjectInformatieObjectenQuery { GetAllObjectInformatieObjectenFilter = filter });

        var objectInformatieObjectenResponse = _mapper.Map<List<ObjectInformatieObjectResponseDto>>(result.Result);

        // TODO: Still deciding if this makes sense (because can generate lot of audittrail logs)
        //await _mediator.Send(new LogAuditTrailGetObjectListCommand
        //{
        //    RetrieveCatagory = RetrieveCatagory.All,
        //    TotalCount = result.Result.Count,
        //    AuditTrailOptions = new AuditTrailOptions { Bron = "DRC", Resource = "objectinformatieobject" }
        //});

        return Ok(objectInformatieObjectenResponse);
    }

    /// <summary>
    /// Een specifieke OBJECT-INFORMATIEOBJECT relatie opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ObjectInformatieObjecten.Get, Name = Operations.ObjectInformatieObjecten.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ObjectInformatieObjectResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    public async Task<IActionResult> GetAsync(Guid id)
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

        var objectInformatieObjectResponse = _mapper.Map<ObjectInformatieObjectResponseDto>(result.Result);

        // TODO: Still deciding if this makes sense (because can generate lot of audittrail logs)
        //await _mediator.Send(new LogAuditTrailGetObjectCommand
        //{
        //    RetrieveCatagory = RetrieveCatagory.All,
        //    BaseEntity = result.Result.InformatieObject,
        //    SubEntity = result.Result,
        //    AuditTrailOptions = new AuditTrailOptions { Bron = "DRC", Resource = "objectinformatieobject" }
        //});

        return Ok(objectInformatieObjectResponse);
    }

    //
    // HTTP POST http://documenten.user.local:5007/api/v1/objectinformatieobjecten

    /// <summary>
    /// Maak een OBJECT-INFORMATIEOBJECT relatie aan.
    /// </summary>
    /// <remarks>
    /// LET OP: Dit endpoint hoor je als consumer niet zelf aan te spreken.
    /// Andere API's, zoals de Zaken API en de Besluiten API, gebruiken dit endpoint bij het synchroniseren van relaties.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ObjectInformatieObjecten.Create, Name = Operations.ObjectInformatieObjecten.Create)]
    [Scope(AuthorizationScopes.Documenten.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ObjectInformatieObjectResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> AddAsync([FromBody] ObjectInformatieObjectRequestDto objectInformatieObjectRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), objectInformatieObjectRequest);

        ObjectInformatieObject objectInformatieObject = _mapper.Map<ObjectInformatieObject>(objectInformatieObjectRequest);

        var result = await _mediator.Send(
            new CreateObjectInformatieObjectCommand
            {
                ObjectInformatieObject = objectInformatieObject,
                InformatieObjectUrl = objectInformatieObjectRequest.InformatieObject,
            }
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var objectInformatieObjectResponse = _mapper.Map<ObjectInformatieObjectResponseDto>(result.Result);

        return Created(objectInformatieObjectResponse.Url, objectInformatieObjectResponse);
    }

    //
    // HTTP DELETE http://zaken.user.local:5007/api/v1/objectinformatieobjecten/59bad509-840b-4cd0-82dc-cbda74a75c2b

    /// <summary>
    /// Verwijder een OBJECT-INFORMATIEOBJECT relatie.
    /// </summary>
    /// <remarks>
    /// LET OP: Dit endpoint hoor je als consumer niet zelf aan te spreken.
    /// Andere API's, zoals de Zaken API en de Besluiten API, gebruiken dit endpoint bij het synchroniseren van relaties.
    /// </remarks>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ObjectInformatieObjecten.Delete, Name = Operations.ObjectInformatieObjecten.Delete)]
    [Scope(AuthorizationScopes.Documenten.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteObjectInformatieObjectCommand { Id = id });

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
