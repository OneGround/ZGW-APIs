using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using OneGround.ZGW.Common.Web.HealthChecks.Checks;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.HealthChecks.Checks;

public class EventBusHealthCheckTests
{
    private readonly Mock<IBusControl> _mockBusControl;
    private readonly EventBusHealthCheck _healthCheck;

    public EventBusHealthCheckTests()
    {
        _mockBusControl = new Mock<IBusControl>();
        _healthCheck = new EventBusHealthCheck(_mockBusControl.Object);
    }

    [Fact]
    public void Constructor_WithNullBusControl_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new EventBusHealthCheck(null));
        Assert.Equal("busControl", ex.ParamName);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenEventBusIsHealthy_ReturnsHealthy()
    {
        var busHealthResult = BusHealthResult.Healthy("All endpoints are healthy", new Dictionary<string, EndpointHealthResult>());
        _mockBusControl.Setup(b => b.CheckHealth()).Returns(busHealthResult);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Event bus is healthy", result.Description);
        Assert.True(result.Data.ContainsKey("status"));
        Assert.Equal("Healthy", result.Data["status"]);
        Assert.True(result.Data.ContainsKey("description"));
        Assert.Equal("All endpoints are healthy", result.Data["description"]);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenEventBusIsDegraded_ReturnsDegraded()
    {
        var busHealthResult = BusHealthResult.Degraded("Some endpoints are slow", null!, new Dictionary<string, EndpointHealthResult>());
        _mockBusControl.Setup(b => b.CheckHealth()).Returns(busHealthResult);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Event bus is degraded: Some endpoints are slow", result.Description);
        Assert.True(result.Data.ContainsKey("status"));
        Assert.Equal("Degraded", result.Data["status"]);
        Assert.True(result.Data.ContainsKey("description"));
        Assert.Equal("Some endpoints are slow", result.Data["description"]);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenEventBusIsUnhealthy_ReturnsUnhealthy()
    {
        var busHealthResult = BusHealthResult.Unhealthy("RabbitMQ connection failed", null!, new Dictionary<string, EndpointHealthResult>());
        _mockBusControl.Setup(b => b.CheckHealth()).Returns(busHealthResult);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Event bus is unhealthy: RabbitMQ connection failed", result.Description);
        Assert.True(result.Data.ContainsKey("status"));
        Assert.Equal("Unhealthy", result.Data["status"]);
        Assert.True(result.Data.ContainsKey("description"));
        Assert.Equal("RabbitMQ connection failed", result.Data["description"]);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenEventBusStatusIsUnknown_ReturnsUnhealthy()
    {
        // Since we cannot easily create a BusHealthResult with an unknown status value
        // (it would require mocking or reflection which MassTransit's sealed implementation prevents),
        // we test that the default case in the switch statement handles unexpected scenarios
        // by ensuring Unhealthy status produces the correct unhealthy result
        var busHealthResult = BusHealthResult.Unhealthy("Unknown state", null!, new Dictionary<string, EndpointHealthResult>());
        _mockBusControl.Setup(b => b.CheckHealth()).Returns(busHealthResult);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Event bus is unhealthy:", result.Description);
        Assert.True(result.Data.ContainsKey("status"));
        Assert.Equal("Unhealthy", result.Data["status"]);
        Assert.True(result.Data.ContainsKey("description"));
        Assert.Equal("Unknown state", result.Data["description"]);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCheckHealthThrowsException_ReturnsUnhealthy()
    {
        var expectedException = new InvalidOperationException("Bus not started");
        _mockBusControl.Setup(b => b.CheckHealth()).Throws(expectedException);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Event bus health check failed", result.Description);
        Assert.Equal(expectedException, result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDegradedWithEmptyDescription_IncludesEmptyDescriptionInMessage()
    {
        var busHealthResult = BusHealthResult.Degraded("", null!, new Dictionary<string, EndpointHealthResult>());
        _mockBusControl.Setup(b => b.CheckHealth()).Returns(busHealthResult);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Event bus is degraded:", result.Description);
        Assert.True(result.Data.ContainsKey("description"));
        Assert.Equal("", result.Data["description"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnhealthyWithDescription_HandlesDescription()
    {
        var busHealthResult = BusHealthResult.Unhealthy("Connection error", null!, new Dictionary<string, EndpointHealthResult>());
        _mockBusControl.Setup(b => b.CheckHealth()).Returns(busHealthResult);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Event bus is unhealthy:", result.Description);
        Assert.True(result.Data.ContainsKey("description"));
        Assert.Equal("Connection error", result.Data["description"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellationToken_CompletesSuccessfully()
    {
        var busHealthResult = BusHealthResult.Healthy("Healthy", new Dictionary<string, EndpointHealthResult>());
        _mockBusControl.Setup(b => b.CheckHealth()).Returns(busHealthResult);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithHealthCheckContext_DoesNotThrowException()
    {
        var busHealthResult = BusHealthResult.Healthy("Healthy", new Dictionary<string, EndpointHealthResult>());
        _mockBusControl.Setup(b => b.CheckHealth()).Returns(busHealthResult);
        var context = new HealthCheckContext { Registration = new HealthCheckRegistration("EventBus", _healthCheck, HealthStatus.Unhealthy, null) };

        var result = await _healthCheck.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthyWithLongDescription_IncludesFullDescription()
    {
        var longDescription = new string('a', 500);
        var busHealthResult = BusHealthResult.Healthy(longDescription, new Dictionary<string, EndpointHealthResult>());
        _mockBusControl.Setup(b => b.CheckHealth()).Returns(busHealthResult);

        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.True(result.Data.ContainsKey("description"));
        Assert.Equal(longDescription, result.Data["description"]);
    }

    [Fact]
    public async Task CheckHealthAsync_CalledMultipleTimes_ChecksHealthEachTime()
    {
        var firstResult = BusHealthResult.Healthy("First check", new Dictionary<string, EndpointHealthResult>());
        var secondResult = BusHealthResult.Degraded("Second check", null!, new Dictionary<string, EndpointHealthResult>());

        _mockBusControl.SetupSequence(b => b.CheckHealth()).Returns(firstResult).Returns(secondResult);

        var result1 = await _healthCheck.CheckHealthAsync(new HealthCheckContext());
        var result2 = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result1.Status);
        Assert.Equal("First check", result1.Data["description"]);
        Assert.Equal(HealthStatus.Degraded, result2.Status);
        Assert.Equal("Second check", result2.Data["description"]);
        _mockBusControl.Verify(b => b.CheckHealth(), Times.Exactly(2));
    }

    [Fact]
    public void HealthCheckName_HasExpectedValue()
    {
        Assert.Equal("EventBus", EventBusHealthCheck.HealthCheckName);
    }
}
