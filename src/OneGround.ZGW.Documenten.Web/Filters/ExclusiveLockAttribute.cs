using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Documenten.Services;

namespace OneGround.ZGW.Documenten.Web.Filters;

/// <summary>
/// Applies distributed locking to controller actions based on a parameter value.
/// </summary>
/// <example>
/// <code>
/// [HttpPut("{id}")]
/// [ExclusiveLock("id")]
/// public async Task&lt;IActionResult&gt; UpdateAsync([FromBody] RequestDto request, Guid id)
/// {
///     // Action code - lock is automatically acquired before this executes
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class ExclusiveLockAttribute : Attribute, IFilterFactory
{
    private readonly string _parameterName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExclusiveLockAttribute"/> class.
    /// </summary>
    /// <param name="parameterName">
    /// The name of the action parameter to use as the lock key.
    /// Must be of type <see cref="Guid"/> or <see cref="long"/>.
    /// </param>
    public ExclusiveLockAttribute(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            throw new ArgumentException("Parameter name cannot be null or empty.", nameof(parameterName));
        }

        _parameterName = parameterName;
    }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        var logger = serviceProvider.GetRequiredService<ILogger<ExclusiveLockFilter>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        return new ExclusiveLockFilter(lockFactory, logger, configuration, _parameterName);
    }
}

/// <summary>
/// Action filter that acquires and releases distributed locks for controller actions.
/// </summary>
internal class ExclusiveLockFilter : IAsyncActionFilter
{
    private readonly IDistributedLockFactory _lockFactory;
    private readonly ILogger<ExclusiveLockFilter> _logger;
    private readonly TimeSpan _acquireTimeout;
    private readonly string _parameterName;

    public ExclusiveLockFilter(
        IDistributedLockFactory lockFactory,
        ILogger<ExclusiveLockFilter> logger,
        IConfiguration configuration,
        string parameterName
    )
    {
        _lockFactory = lockFactory;
        _logger = logger;
        _parameterName = parameterName;

        // Load timeout from configuration, default to 5 seconds if not specified
        _acquireTimeout = configuration.GetValue<TimeSpan?>("DistributedLock:AcquireLockTimeout") ?? TimeSpan.FromSeconds(5);
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var lockKey = GetLockKey(context);
        if (lockKey == null)
        {
            _logger.LogError(
                "Failed to extract lock key from parameter '{ParameterName}' in action '{ActionName}'. " + "Parameter must be of type Guid or long.",
                _parameterName,
                context.ActionDescriptor.DisplayName
            );

            context.Result = new BadRequestObjectResult(
                new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Bad Request",
                    status = 400,
                    detail = $"Invalid lock parameter '{_parameterName}'.",
                    instance = $"urn:uuid:{Guid.NewGuid()}",
                }
            );
            return;
        }

        // Try to acquire the distributed lock
        await using var distLock = CreateLock(lockKey.Value);

        var cancellationToken = context.HttpContext.RequestAborted;
        bool acquired = await distLock.AcquireAsync(_acquireTimeout, cancellationToken);

        if (!acquired)
        {
            var resourceId = lockKey.Value.guid.HasValue ? (object)lockKey.Value.guid.Value : lockKey.Value.longKey;

            _logger.LogWarning(
                "Failed to acquire lock for {ResourceType} ({ResourceId}) within {Timeout}. " + "Another process is holding the lock.",
                GetResourceType(context),
                resourceId,
                _acquireTimeout
            );

            context.Result = CreateLockedResponse(resourceId);
            return;
        }

        var logResourceId = lockKey.Value.guid.HasValue ? (object)lockKey.Value.guid.Value : lockKey.Value.longKey;

        _logger.LogDebug("Acquired exclusive lock for {ResourceType} ({ResourceId})", GetResourceType(context), logResourceId);

        // Execute the action with the lock held
        var executedContext = await next();

        _logger.LogDebug("Released exclusive lock for {ResourceType} ({ResourceId})", GetResourceType(context), logResourceId);

        // Lock is automatically released when distLock is disposed
    }

    /// <summary>
    /// Extracts the lock key from the action arguments.
    /// </summary>
    private (Guid? guid, long longKey)? GetLockKey(ActionExecutingContext context)
    {
        var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        if (controllerActionDescriptor == null)
        {
            return null;
        }

        // Find the parameter by name
        if (!context.ActionArguments.TryGetValue(_parameterName, out var parameterValue))
        {
            _logger.LogError("Parameter '{ParameterName}' not found in action '{ActionName}'", _parameterName, controllerActionDescriptor.ActionName);
            return null;
        }

        // Support both Guid and long parameter types
        return parameterValue switch
        {
            Guid guid => (guid, 0L),
            long longKey => (null, longKey),
            _ => null,
        };
    }

    /// <summary>
    /// Creates a distributed lock based on the key type.
    /// </summary>
    private IDistributedLock CreateLock((Guid? guid, long longKey) lockKey)
    {
        return lockKey.guid.HasValue ? _lockFactory.Create(lockKey.guid.Value) : _lockFactory.Create(lockKey.longKey);
    }

    /// <summary>
    /// Gets a human-readable resource type name for logging.
    /// </summary>
    private string GetResourceType(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            return descriptor.ControllerName;
        }
        return "Resource";
    }

    /// <summary>
    /// Creates a 423 Locked response when lock acquisition fails.
    /// </summary>
    private IActionResult CreateLockedResponse(object resourceId)
    {
        return new ObjectResult(
            new
            {
                type = "https://documenten.user.local/ref/fouten/LockError/",
                code = "locked",
                title = "Resource already locked.",
                status = (int)HttpStatusCode.Locked,
                detail = $"Another process has already exclusive access to resource {resourceId}.",
                instance = $"urn:uuid:{Guid.NewGuid()}",
            }
        )
        {
            StatusCode = (int)HttpStatusCode.Locked,
        };
    }
}
