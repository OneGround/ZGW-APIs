using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Common.Web.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[ApiVersionNeutral]
[ScopeNotRequired("Target of UseExceptionHandler(\"/error\"); runs after the pipeline failed, so no client scope is resolvable.")]
public class ErrorController : ControllerBase
{
    private readonly IErrorResponseBuilder _errorResponseBuilder;

    public ErrorController(IErrorResponseBuilder errorResponseBuilder)
    {
        _errorResponseBuilder = errorResponseBuilder;
    }

    [Route("/error")]
    public IActionResult Error() => _errorResponseBuilder.InternalServerError();
}
