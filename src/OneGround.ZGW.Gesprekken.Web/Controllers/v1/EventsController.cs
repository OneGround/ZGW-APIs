using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Authorization;
using OneGround.ZGW.Gesprekken.Web.Configuration;
using OneGround.ZGW.Gesprekken.Web.Contracts.v1;
using OneGround.ZGW.Gesprekken.Web.Events;
using OneGround.ZGW.Gesprekken.Web.Helpers;

namespace OneGround.ZGW.Gesprekken.Web.Controllers.v1;

/// <summary>
/// The event stream (Server-Sent Events, one CloudEvent per change). A client takes a snapshot through the read
/// endpoints, takes the Feed-Cursor header of the first response, and follows this stream with that cursor as
/// Last-Event-ID. Writing stays on the REST endpoints.
/// </summary>
[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class EventsController : ZGWControllerBase
{
    private const int BatchSize = 100;

    private readonly IGesprekEventReader _reader;
    private readonly IAuthorizationContextAccessor _authorizationContextAccessor;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public EventsController(
        ILogger<EventsController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IErrorResponseBuilder errorResponseBuilder,
        IGesprekEventReader reader,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IConfiguration configuration
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _reader = reader;
        _authorizationContextAccessor = authorizationContextAccessor;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>() ?? new ApplicationConfiguration();
    }

    /// <summary>
    /// Wijzigingen volgen als Server-Sent Events. Zonder Last-Event-ID levert de stroom alleen nieuwe events.
    /// </summary>
    /// <response code="400">Last-Event-ID is not a cursor</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="410">The cursor is too old; take a new snapshot</response>
    [HttpGet(ApiRoutes.Events.Stream, Name = Operations.Events.Stream)]
    [Scope(AuthorizationScopes.Gesprekken.Read)]
    [Produces("text/event-stream")]
    public async Task StreamAsync(
        [FromQuery(Name = "gesprek")] string[] gesprek,
        [FromQuery(Name = "aanleidingZaak")] string aanleidingZaak,
        [FromHeader(Name = "Last-Event-ID")] string lastEventId,
        CancellationToken cancellationToken
    )
    {
        var rsin = _authorizationContextAccessor.AuthorizationContext.Authorization.Rsin;

        long cursor;
        if (string.IsNullOrEmpty(lastEventId))
        {
            cursor = await _reader.GetHeadAsync(rsin, cancellationToken);
        }
        else if (!long.TryParse(lastEventId, out cursor) || cursor < 0)
        {
            await WriteErrorAsync(StatusCodes.Status400BadRequest, "invalid", "Last-Event-ID is geen geldige cursor.");
            return;
        }
        else if (await _reader.GetOldestAsync(rsin, cancellationToken) is { } oldest && cursor < oldest - 1)
        {
            await WriteErrorAsync(
                StatusCodes.Status410Gone,
                "cursor-verlopen",
                "De events na deze cursor zijn niet meer beschikbaar. Haal een nieuwe snapshot op."
            );
            return;
        }

        var gesprekken = new List<Guid>();
        foreach (var url in gesprek ?? [])
        {
            if (!GrcUrl.TryGetId(url, GrcUrl.Gesprekken, out var id))
            {
                await WriteErrorAsync(StatusCodes.Status400BadRequest, "invalid", "gesprek is geen URL van een gesprek.");
                return;
            }

            gesprekken.Add(id);
        }

        var filter = new GesprekEventFilter { Gesprekken = gesprekken, AanleidingZaak = aanleidingZaak?.TrimEnd('/') };
        var source = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v1";

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        await Response.Body.FlushAsync(cancellationToken);

        var lastWrite = DateTime.UtcNow;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var events = await _reader.ReadAfterAsync(rsin, cursor, filter, BatchSize, cancellationToken);
                foreach (var record in events)
                {
                    await WriteAsync(FormatEvent(record, source), cancellationToken);
                    cursor = record.Sequence;
                }

                if (events.Count > 0)
                {
                    lastWrite = DateTime.UtcNow;
                    continue;
                }

                if (DateTime.UtcNow - lastWrite >= TimeSpan.FromSeconds(_applicationConfiguration.EventStreamPingSeconds))
                {
                    await WriteAsync(": ping\n\n", cancellationToken);
                    lastWrite = DateTime.UtcNow;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(_applicationConfiguration.EventStreamPollMilliseconds), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The client closed the connection
        }
    }

    private static string FormatEvent(GesprekEventRecord record, string source)
    {
        var cloudEvent = new JObject
        {
            ["specversion"] = "1.0",
            ["id"] = record.EventId.ToString(),
            ["source"] = source,
            ["type"] = record.Type,
            ["subject"] = record.Subject,
            ["time"] = record.Time.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["sequence"] = record.Sequence.ToString(),
            ["datacontenttype"] = "application/json",
            ["data"] = JToken.Parse(record.Data),
        };

        // Extension attribute: lets a consumer route every event of a gesprek to its zaak without a lookup
        if (record.AanleidingZaak != null)
        {
            cloudEvent["aanleidingzaak"] = record.AanleidingZaak;
        }

        return $"id: {record.Sequence}\nevent: {record.Type}\ndata: {cloudEvent.ToString(Formatting.None)}\n\n";
    }

    private async Task WriteAsync(string text, CancellationToken cancellationToken)
    {
        await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(text), cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private async Task WriteErrorAsync(int status, string code, string detail)
    {
        Response.StatusCode = status;
        Response.ContentType = "application/problem+json";
        var error = new ErrorResponse
        {
            Type = $"{code}",
            Code = code,
            Title = detail,
            Status = status,
            Detail = detail,
        };
        await Response.WriteAsync(JsonConvert.SerializeObject(error));
    }
}
