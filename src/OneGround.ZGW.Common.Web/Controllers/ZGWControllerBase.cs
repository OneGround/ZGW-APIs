using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Controllers;

public abstract class ZGWControllerBase : ControllerBase
{
    protected readonly ILogger _logger;
    protected readonly IMediator _mediator;
    protected readonly IMapper _mapper;
    protected readonly IRequestMerger _requestMerger;
    protected readonly IErrorResponseBuilder _errorResponseBuilder;

    protected ZGWControllerBase(
        ILogger logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
    {
        _logger = logger;
        _mediator = mediator;
        _mapper = mapper;
        _requestMerger = requestMerger;
        _errorResponseBuilder = errorResponseBuilder;
    }

    protected string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    protected bool IsApiVersionRequested(string version)
    {
        var apiVersionHeader = HttpContext.Request.Headers.SingleOrDefault(h =>
            string.Equals(h.Key, "Api-Version", StringComparison.OrdinalIgnoreCase)
        );

        return apiVersionHeader.Value == version;
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
}
