using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Controllers;

public abstract class ZGWControllerBase : ControllerBase
{
    protected readonly ILogger _logger;
    protected readonly IMediator _mediator;
    protected readonly IMapper _mapper;
    protected readonly IErrorResponseBuilder _errorResponseBuilder;

    protected ZGWControllerBase(ILogger logger, IMediator mediator, IMapper mapper, IErrorResponseBuilder errorResponseBuilder)
    {
        _logger = logger;
        _mediator = mediator;
        _mapper = mapper;
        _errorResponseBuilder = errorResponseBuilder;
    }

    protected string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    protected bool IsApiVersionRequested(ApiVersion version)
    {
        return HttpContext?.GetRequestedApiVersion() >= version;
    }

    protected int? TryGetSridFromContentCrsHeader()
    {
        var acceptCrs = HttpContext.GetAcceptCrsHeader();
        var contentCrs = HttpContext.GetContentCrsHeader();

        if (acceptCrs != contentCrs) // Note: We don't support conversion between what we requested and what we want in the response (for now)
            return null;

        if (contentCrs == "EPSG:4326")
            return 4326;
        if (contentCrs == "EPSG:28992")
            return 28992;
        if (contentCrs == "EPSG:4937")
            return 4937;
        throw new NotImplementedException($"A not supported contentCrs header {contentCrs}.");
    }

    protected int GetSridFromAcceptCrsHeader()
    {
        var acceptCrs = HttpContext.GetAcceptCrsHeader();

        if (acceptCrs == "EPSG:4326")
            return 4326;
        if (acceptCrs == "EPSG:28992")
            return 28992;
        if (acceptCrs == "EPSG:4937")
            return 4937;
        throw new NotImplementedException($"A not supported acceptCrs header {acceptCrs}.");
    }

    protected static HashSet<string> ExpandLookup(string expand) =>
        expand != null ? expand.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet() : [];

    // Note: Temporary log method. We should investigate who send the 2-letter language code so we log for these situations
    protected void LogInvalidTaalCode(string taalRequested, string taalMapped)
    {
        if (taalRequested?.Length != taalMapped?.Length)
        {
            var clientId =
                User.Claims.FirstOrDefault(c => c.Type.Equals(CustomClaimTypes.ClientId, StringComparison.OrdinalIgnoreCase))?.Value ?? "unknown";

            _logger.LogWarning(
                "Language code mismatch: Request has {RequestLength}-character code '{RequestTaal}', but mapped to {MappedLength}-character code '{MappedTaal}' for ClientId: {ClientId}",
                taalRequested?.Length ?? 0,
                taalRequested ?? "null",
                taalMapped?.Length ?? 0,
                taalMapped ?? "null",
                clientId
            );
        }
    }

    protected static T GetValueFromPartial<T>(dynamic jsonObject, string name, bool caseSensitive = false)
    {
        var value = jsonObject.Property(name, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)?.Value;

        if (value == null)
            return default;

        return value.ToObject<T>();
    }

    protected IActionResult ExterneServiceFout(string serviceName, string serviceUrl)
    {
        return _errorResponseBuilder.BadGateway(
            code: ErrorCode.ExternalServiceError,
            title: $"Externe service '{serviceName}' niet beschikbaar",
            detail: $"De expand kon niet worden uitgevoerd omdat service '{serviceName}' niet bereikbaar is of een fout heeft teruggegeven voor URL '{serviceUrl}'."
        );
    }

    protected IActionResult InterneQueryHandlerFout(string resource, QueryStatus statuscode)
    {
        switch (statuscode)
        {
            case QueryStatus.NotFound:
                return _errorResponseBuilder.NotFound([
                    new ValidationError(
                        name: resource,
                        code: ErrorCode.NotFound,
                        reason: "De expand kon niet worden uitgevoerd omdat de interne Query-handler een 'not found' fout heeft teruggegeven."
                    ),
                ]);

            case QueryStatus.Forbidden:
                return _errorResponseBuilder.Forbidden([
                    new ValidationError(
                        name: resource,
                        code: ErrorCode.Forbidden,
                        reason: "De expand kon niet worden uitgevoerd omdat de interne Query-handler een 'forbidden' fout heeft teruggegeven."
                    ),
                ]);
        }
        return _errorResponseBuilder.InternalServerError();
    }

    protected bool IsExpandEnabled(string allowedExpand, IList<string> specifiedExpands)
    {
        if (specifiedExpands.Count == 0 || string.IsNullOrEmpty(allowedExpand))
            return true;

        switch (allowedExpand)
        {
            case "all":
                return true;
            case "none":
                return false;
        }

        var allowedExpandsLookup = allowedExpand.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        return specifiedExpands.All(expand => allowedExpandsLookup.ContainsAnyOf(expand));
    }

    /// <summary>
    /// Parses and validates the "expand" query parameter, and checks whether the resulting paths are allowed on this operation.
    /// Returns the error result to return to the caller, or null when validation succeeded (in which case <paramref name="expandPaths"/> is populated).
    /// </summary>
    protected IActionResult ValidateExpand<TEntity>(
        ExpandValidator<TEntity> expandValidator,
        string expand,
        string allowedExpand,
        out List<string> expandPaths
    )
    {
        var (paths, expandError) = expandValidator.ParseAndValidate(expand);
        if (expandError is not null)
        {
            expandPaths = [];
            return _errorResponseBuilder.BadRequest(
                new[] { new ValidationError("expand", ErrorCode.Invalid, expandError) },
                title: "Ongeldige expand parameter"
            );
        }

        expandPaths = paths;

        if (!IsExpandEnabled(allowedExpand, paths))
        {
            return _errorResponseBuilder.BadRequest(
                new[] { new ValidationError("expand", ErrorCode.DisabledExpand, "Expand is uitgeschakeld op deze operatie.") },
                title: "Invalid input"
            );
        }

        return null;
    }
}
