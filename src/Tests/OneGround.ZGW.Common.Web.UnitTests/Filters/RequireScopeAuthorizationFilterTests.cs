using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Services;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Filters;

public class RequireScopeAuthorizationFilterTests
{
    private static readonly JsonResult ForbiddenResult = new(new { }) { StatusCode = (int)HttpStatusCode.Forbidden };

    private readonly RecordingLogger _logger = new();
    private readonly Mock<IErrorResponseBuilder> _errorResponseBuilder = new();
    private readonly RequireScopeAuthorizationFilter _filter;

    public RequireScopeAuthorizationFilterTests()
    {
        _errorResponseBuilder.Setup(b => b.Forbidden()).Returns(ForbiddenResult);
        _filter = new RequireScopeAuthorizationFilter(_logger, _errorResponseBuilder.Object);
    }

    [Fact]
    public async Task An_action_carrying_a_scope_attribute_is_let_through()
    {
        var context = CreateContext(typeof(FakeController), nameof(FakeController.Scoped));

        await _filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        Assert.Empty(_logger.Errors);
    }

    [Fact]
    public async Task An_action_marked_ScopeNotRequired_is_let_through()
    {
        var context = CreateContext(typeof(FakeController), nameof(FakeController.Exempt));

        await _filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        Assert.Empty(_logger.Errors);
    }

    [Fact]
    public async Task An_action_carrying_neither_is_denied_with_the_403_from_the_error_response_builder()
    {
        var context = CreateContext(typeof(FakeController), nameof(FakeController.Bare));

        await _filter.OnAuthorizationAsync(context);

        var result = Assert.IsType<JsonResult>(context.Result);
        Assert.Same(ForbiddenResult, result);
        Assert.Equal((int)HttpStatusCode.Forbidden, result.StatusCode);
        _errorResponseBuilder.Verify(b => b.Forbidden(), Times.Once);
    }

    [Fact]
    public async Task A_denied_action_is_logged_at_error_level_naming_the_controller_and_the_action()
    {
        var context = CreateContext(typeof(FakeController), nameof(FakeController.Bare));

        await _filter.OnAuthorizationAsync(context);

        var logged = Assert.Single(_logger.Errors);
        Assert.Contains(nameof(FakeController), logged);
        Assert.Contains(nameof(FakeController.Bare), logged);
    }

    [Fact]
    public async Task A_class_level_exemption_covers_the_actions_of_that_controller()
    {
        var context = CreateContext(typeof(ExemptFakeController), nameof(ExemptFakeController.Bare));

        await _filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task A_non_controller_action_descriptor_is_left_untouched()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, []);

        await _filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        Assert.Empty(_logger.Errors);
        _errorResponseBuilder.Verify(b => b.Forbidden(), Times.Never);
    }

    [Fact]
    public async Task The_error_endpoint_is_let_through()
    {
        var context = CreateContext(typeof(ErrorController), nameof(ErrorController.Error));

        await _filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task An_unhandled_exception_re_executed_through_the_error_endpoint_still_yields_500()
    {
        // UseExceptionHandler("/error") re-executes the request on ErrorController.Error. The filter must not
        // short-circuit that action, or every unhandled 500 would surface to the client as a 403 instead.
        var context = CreateContext(typeof(ErrorController), nameof(ErrorController.Error));

        await _filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);

        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var result = Assert.IsType<JsonResult>(new ErrorController(new ErrorResponseBuilder(httpContextAccessor)).Error());

        Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
    }

    private static AuthorizationFilterContext CreateContext(Type controllerType, string actionName)
    {
        var action = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(action);

        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            MethodInfo = action,
            ControllerName = controllerType.Name,
            ActionName = actionName,
        };

        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor);

        return new AuthorizationFilterContext(actionContext, []);
    }

    private sealed class FakeScopeAttribute : BaseScopeAttribute
    {
        public FakeScopeAttribute()
            : base("TEST", "test.lezen") { }
    }

    private class FakeController : ControllerBase
    {
        [FakeScope]
        public IActionResult Scoped() => Ok();

        [ScopeNotRequired("test")]
        public IActionResult Exempt() => Ok();

        public IActionResult Bare() => Ok();
    }

    [ScopeNotRequired("test")]
    private sealed class ExemptFakeController : ControllerBase
    {
        public IActionResult Bare() => Ok();
    }

    private sealed class RecordingLogger : ILogger<RequireScopeAuthorizationFilter>
    {
        public List<string> Errors { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                Errors.Add(formatter(state, exception));
            }
        }
    }
}
