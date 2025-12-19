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
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1._6.Requests;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Contracts.v1._6;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1._6;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_6)]
[Consumes("application/json")]
[Produces("application/json")]
public class ZakenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly IObjectExpander<Zaken.Contracts.v1._5.Responses.ZaakResponseDto> _expander;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZakenController(
        ILogger<ZakenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder,
        IExpanderFactory expanderFactory
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _expander = expanderFactory.Create<Zaken.Contracts.v1._5.Responses.ZaakResponseDto>("zaak");
    }

    /// <summary>
    /// Voer een (geo)-zoekopdracht uit op ZAAKen.
    /// </summary>
    /// <remarks>
    /// Zoeken/filteren gaat normaal via de list operatie, deze is echter niet geschikt voor geo-zoekopdrachten.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="412">Precondition Failed</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Zaken.Search, Name = Operations.Zaken.Search)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<Zaken.Contracts.v1._5.Responses.ZaakResponseDto>))]
    [RequiresAcceptCrs]
    [Expand]
    public async Task<IActionResult> SearchAsync(ZaakSearchRequestDto zaakSearchRequest, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Page}", nameof(SearchAsync), zaakSearchRequest, page);

        if (zaakSearchRequest?.Body == null)
        {
            return _errorResponseBuilder.BadRequest([new ValidationError(name: "body", ErrorCode.Required, reason: "Body is required.")]);
        }

        var parser = new ExpandParser(rootName: "zaak", zaakSearchRequest.Body);
        if (!parser.IsValid("zaak"))
        {
            return _errorResponseBuilder.BadRequest(
                [new ValidationError(name: "body", ErrorCode.Required, reason: "Fields/expand selectie is requied.")]
            );
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZakenPageSize));

        var typedZaakSearchRequest = new ZaakSearchRequestTypedDto(zaakSearchRequest.Body);

        var filter = _mapper.Map<Models.v1._5.GetAllZakenFilter>(typedZaakSearchRequest);

        var result = await _mediator.Send(
            new Handlers.v1._5.GetAllZakenQuery
            {
                GetAllZakenFilter = filter,
                WithinZaakGeometry = typedZaakSearchRequest.ZaakGeometry?.Within,
                Pagination = pagination,
                Ordering = typedZaakSearchRequest.Ordering,
                SRID = GetSridFromAcceptCrsHeader(),
            }
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zakenResponse = _mapper.Map<List<Zaken.Contracts.v1._5.Responses.ZaakResponseDto>>(result.Result.PageResult);

        // Lijst met included properties properties
        var allowedProps = parser.Items["zaak"];

        var zakenWithOptionalExpand = zakenResponse
            .Select(z =>
            {
                var zaakWithOptionalExpands = _expander.ResolveAsync(parser, z).Result;

                var zaakWithExpandsAndLimitedFields = JObjectFilter.FilterObjectByPaths(
                    JObjectHelper.FromObjectOrDefault(zaakWithOptionalExpands, GeometryConfiguredSerializer),
                    allowedProps
                );

                return zaakWithExpandsAndLimitedFields;
            })
            .ToList();

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(pagination, zakenWithOptionalExpand, result.Result.Count);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaak" },
            }
        );

        return Ok(paginationResponse);
    }

    [HttpPost(ApiRoutes.Zaken.GetUitgebreid, Name = Operations.Zaken.ReadUitgebreid)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    public async Task<IActionResult> GetUitgebreid(ZaakUitgebreidRequestDto zaakUitgebreidRequest)
    {
        if (zaakUitgebreidRequest.Body == null)
        {
            return _errorResponseBuilder.BadRequest([new ValidationError(name: "body", ErrorCode.Required, reason: "Body is required.")]);
        }

        if (!zaakUitgebreidRequest.Body.TryGet<Guid>("id", out var id))
        {
            return _errorResponseBuilder.BadRequest(
                [new ValidationError(name: "body", ErrorCode.Required, reason: "Body contains not the required field 'id'.")]
            );
        }

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakQuery { Id = id, SRID = GetSridFromAcceptCrsHeader() });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaak = _mapper.Map<Zaken.Contracts.v1._5.Responses.ZaakResponseDto>(result.Result);

        var parser = new ExpandParser(rootName: "zaak", zaakUitgebreidRequest.Body);

        // Lijst met included properties properties
        var allowedProps = parser.Items["zaak"];

        var zaakWithOptionalExpands = await _expander.ResolveAsync(parser, zaak);

        var zaakWithExpandsAndLimitedFields = JObjectFilter.FilterObjectByPaths(
            JObjectHelper.FromObjectOrDefault(zaakWithOptionalExpands, GeometryConfiguredSerializer),
            allowedProps
        );

        return Ok(zaakWithExpandsAndLimitedFields);
    }

    private static JsonSerializer GeometryConfiguredSerializer =>
        JsonSerializer.Create(new JsonSerializerSettings { Converters = [new GeometryConverter()] });
}
