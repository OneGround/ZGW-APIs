using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Storage;
using Moq;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Models;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;
using OneGround.ZGW.Notificaties.Messaging.UI;
using StackExchange.Redis;
using Xunit;

namespace OneGround.ZGW.Notificaties.Listener.UnitTests.UI;

// Regression coverage for the OGSEC-56 CSP fix: the dashboard page used to embed its client-side
// behaviour as an inline <script> block plus onclick/onchange/javascript: attributes, all of which
// a script-src 'self' policy blocks. This asserts none of that inline surface can creep back in.
public class UnhealthMonitorDashboardPageTests
{
    [Fact]
    public async Task Rendered_page_carries_no_inline_script_or_event_handler_attributes()
    {
        var healthTrackerMock = new Mock<ICircuitBreakerSubscriberHealthTracker>();
        healthTrackerMock
            .Setup(t => t.GetAllUnhealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<RedisKey, CircuitBreakerSubscriberHealthState>());

        var page = new UnhealthMonitorDashboardPage(healthTrackerMock.Object);
        var response = new FakeDashboardResponse();
        var context = new TestDashboardContext(new FakeJobStorage(), new DashboardOptions(), new FakeDashboardRequest(), response);

        await page.Dispatch(context);

        var html = response.WrittenContent;

        Assert.DoesNotMatch(new Regex("<script>"), html);
        Assert.DoesNotMatch(new Regex("on(click|change)='"), html);
        Assert.DoesNotMatch(new Regex("javascript:"), html);
    }

    // DashboardContext.Request/Response only have a protected setter, so a plain instance can't be
    // wired up from a test; a trivial subclass gets access to assign the fakes below.
    private sealed class TestDashboardContext : DashboardContext
    {
        public TestDashboardContext(JobStorage storage, DashboardOptions options, DashboardRequest request, DashboardResponse response)
            : base(storage, options)
        {
            Request = request;
            Response = response;
        }
    }

    private sealed class FakeJobStorage : JobStorage
    {
        public override IMonitoringApi GetMonitoringApi() => throw new NotSupportedException();

        public override IStorageConnection GetConnection() => throw new NotSupportedException();
    }

    private sealed class FakeDashboardRequest : DashboardRequest
    {
        public override string Method => "GET";
        public override string Path => "/unhealthmonitor";
        public override string PathBase => "/hangfire";
        public override string LocalIpAddress => "127.0.0.1";
        public override string RemoteIpAddress => "127.0.0.1";

        public override string GetQuery(string key) => null;

        public override Task<IList<string>> GetFormValuesAsync(string key) => Task.FromResult<IList<string>>(new List<string>());
    }

    private sealed class FakeDashboardResponse : DashboardResponse
    {
        private readonly StringBuilder _written = new();

        public override string ContentType { get; set; }
        public override int StatusCode { get; set; }
        public override Stream Body => Stream.Null;

        public string WrittenContent => _written.ToString();

        public override void SetExpire(DateTimeOffset? value) { }

        public override Task WriteAsync(string content)
        {
            _written.Append(content);
            return Task.CompletedTask;
        }
    }
}
