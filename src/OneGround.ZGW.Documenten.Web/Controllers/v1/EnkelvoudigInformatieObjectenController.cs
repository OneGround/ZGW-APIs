using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Contracts.v1;
using OneGround.ZGW.Documenten.Web.Handlers.v1;
using Swashbuckle.AspNetCore.Annotations;

//
// Bron DRC API: https://documenten-api.vng.cloud/api/v1/schema/

namespace OneGround.ZGW.Documenten.Web.Controllers.v1;

// Note: The versioned resource endpoints (GetAll/Get/Create/Update/Download/Lock/Unlock) live in the
// per-version controllers (v1/1, v1/5, v1/7). This base controller only hosts the endpoints that never
// changed across versions and are therefore shared by all currently supported versions: Delete (1.1/1.5,
// 1.7 has its own) and the audit-trail reads (all versions).
[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
public class EnkelvoudigInformatieObjectenController : ZGWControllerBase
{
    public EnkelvoudigInformatieObjectenController(
        ILogger<EnkelvoudigInformatieObjectenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, errorResponseBuilder) { }

    //
    // HTTP DELETE https://documenten-api.vng.cloud/api/v1/enkelvoudiginformatieobjecten/b24ee37c-00db-4108-b831-e3b420b35a09

    /// <summary>
    /// Verwijder een (ENKELVOUDIG) INFORMATIEOBJECT.
    /// Verwijder een(ENKELVOUDIG) INFORMATIEOBJECT en alle bijbehorende versies, samen met alle gerelateerde resources binnen deze API.
    /// Dit is alleen mogelijk als er geen OBJECTINFORMATIEOBJECTen relateerd zijn aan het (ENKELVOUDIG) INFORMATIEOBJECT.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="409">EnkelvoudigInformatieObject was modified by another user</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.EnkelvoudigInformatieObjecten.Delete, Name = Operations.EnkelvoudigInformatieObjecten.Delete)]
    [Scope(AuthorizationScopes.Documenten.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteEnkelvoudigInformatieObjectCommand { Id = id }, cancellationToken);

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

        if (result.Status == CommandStatus.Conflict)
        {
            return _errorResponseBuilder.Conflict(result.Errors);
        }

        return NoContent();
    }

    //
    // HTTP GET http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/59bad509-840b-4cd0-82dc-cbda74a75c2b/audittrail

    /// <summary>
    /// Alle audit trail regels behorend bij het INFORMATIEOBJECT
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjectAudittrail.GetAll, Name = Operations.EnkelvoudigInformatieObjectAudittrail.List)]
    [Scope(AuthorizationScopes.AuditTrails.Read)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    [ZgwApiVersion(Api.LatestVersion_1_7)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<AuditTrailRegelDto>))]
    public async Task<IActionResult> GetAllAuditTrailRegelsAsync(Guid enkelvoudiginformatieobject_uuid, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "{ControllerMethod} called with {EnkelvoudigInformatieObjectUuid}",
            nameof(GetAllAuditTrailRegelsAsync),
            enkelvoudiginformatieobject_uuid
        );

        var result = await _mediator.Send(
            new GetAllEnkelvoudigInformatieObjectAuditTrailRegels { EnkelvoudigInformatieObjectId = enkelvoudiginformatieobject_uuid },
            cancellationToken
        );

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var enkelvoudiginformatieobjectAuditTrailRegelsResponse = _mapper.Map<List<AuditTrailRegelDto>>(result.Result);

        return Ok(enkelvoudiginformatieobjectAuditTrailRegelsResponse);
    }

    //
    // HTTP GET http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/59bad509-840b-4cd0-82dc-cbda74a75c2b/audittrail/782b4144-0185-4180-8b59-2ce322dad69d

    /// <summary>
    /// Een specifieke audit trail regel opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjectAudittrail.Get, Name = Operations.EnkelvoudigInformatieObjectAudittrail.Read)]
    [Scope(AuthorizationScopes.AuditTrails.Read)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    [ZgwApiVersion(Api.LatestVersion_1_7)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(AuditTrailRegelDto))]
    public async Task<IActionResult> GetAuditTrailRegelAsync(Guid enkelvoudiginformatieobject_uuid, Guid uuid, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "{ControllerMethod} called with {EnkelvoudigInformatieObjectUuid}, {Uuid}",
            nameof(GetAuditTrailRegelAsync),
            enkelvoudiginformatieobject_uuid,
            uuid
        );

        var result = await _mediator.Send(
            new GetEnkelvoudigInformatieObjectAuditTrailRegel
            {
                EnkelvoudigInformatieObjectId = enkelvoudiginformatieobject_uuid,
                AuditTrailRegelId = uuid,
            },
            cancellationToken
        );

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var enkelvoudiginformatieobjectAuditTrailRegelResponse = _mapper.Map<AuditTrailRegelDto>(result.Result);

        return Ok(enkelvoudiginformatieobjectAuditTrailRegelResponse);
    }
}
