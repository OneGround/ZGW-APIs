using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OneGround.ZGW.Common.Web.HealthChecks;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.HealthChecks;

public class OneGroundHealthChecksResponseWriterTests
{
    [Fact]
    public async Task ResponseWriter_With_All_Healthy_Entries_Returns_Healthy_Status()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Database"] = new(HealthStatus.Healthy, "Database is healthy", TimeSpan.Zero, null, null),
                ["Cache"] = new(HealthStatus.Healthy, "Cache is healthy", TimeSpan.Zero, null, null),
            },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        Assert.Equal("Healthy", result.GetProperty("status").GetString());
        Assert.Equal("", result.GetProperty("reason").GetString());
        Assert.Equal(2, result.GetProperty("details").GetArrayLength());
    }

    [Fact]
    public async Task ResponseWriter_With_Degraded_Entry_Returns_Degraded_Status()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Database"] = new(HealthStatus.Healthy, "Database is healthy", TimeSpan.Zero, null, null),
                ["Cache"] = new(HealthStatus.Degraded, "Cache is slow", TimeSpan.Zero, null, null),
            },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        Assert.Equal("Degraded", result.GetProperty("status").GetString());
        Assert.Equal("Cache is slow", result.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task ResponseWriter_With_Unhealthy_Entry_Returns_Unhealthy_Status()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Database"] = new(HealthStatus.Unhealthy, "Database connection failed", TimeSpan.Zero, null, null),
                ["Cache"] = new(HealthStatus.Healthy, "Cache is healthy", TimeSpan.Zero, null, null),
            },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        Assert.Equal("Unhealthy", result.GetProperty("status").GetString());
        Assert.Equal("Database connection failed", result.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task ResponseWriter_With_Multiple_Unhealthy_Entries_Combines_Reasons()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Database"] = new(HealthStatus.Unhealthy, "Database connection failed", TimeSpan.Zero, null, null),
                ["Cache"] = new(HealthStatus.Degraded, "Cache is slow", TimeSpan.Zero, null, null),
                ["MessageQueue"] = new(HealthStatus.Unhealthy, "Queue unavailable", TimeSpan.Zero, null, null),
            },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        Assert.Equal("Unhealthy", result.GetProperty("status").GetString());
        var reason = result.GetProperty("reason").GetString();
        Assert.Contains("Database connection failed", reason);
        Assert.Contains("Cache is slow", reason);
        Assert.Contains("Queue unavailable", reason);
    }

    [Fact]
    public async Task ResponseWriter_With_Mixed_Statuses_Returns_Worst_Status()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Service1"] = new(HealthStatus.Healthy, "Service1 is healthy", TimeSpan.Zero, null, null),
                ["Service2"] = new(HealthStatus.Degraded, "Service2 is degraded", TimeSpan.Zero, null, null),
                ["Service3"] = new(HealthStatus.Unhealthy, "Service3 failed", TimeSpan.Zero, null, null),
            },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        Assert.Equal("Unhealthy", result.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ResponseWriter_With_Empty_Entries_Returns_Healthy_Status()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.FromSeconds(1));

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        Assert.Equal("Healthy", result.GetProperty("status").GetString());
        Assert.Equal("", result.GetProperty("reason").GetString());
        Assert.Equal(0, result.GetProperty("details").GetArrayLength());
    }

    [Fact]
    public async Task ResponseWriter_Sets_Content_Type_To_Application_Json()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry> { ["Test"] = new(HealthStatus.Healthy, "Test", TimeSpan.Zero, null, null) },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
    }

    [Fact]
    public async Task ResponseWriter_Includes_All_Entry_Details()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Database"] = new(HealthStatus.Healthy, "Database is healthy", TimeSpan.Zero, null, null),
                ["Cache"] = new(HealthStatus.Degraded, "Cache is slow", TimeSpan.Zero, null, null),
            },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);
        var details = result.GetProperty("details");

        Assert.Equal(2, details.GetArrayLength());

        var databaseDetail = details[0];
        Assert.Equal("Database", databaseDetail.GetProperty("name").GetString());
        Assert.Equal("Healthy", databaseDetail.GetProperty("status").GetString());
        Assert.Equal("Database is healthy", databaseDetail.GetProperty("reason").GetString());

        var cacheDetail = details[1];
        Assert.Equal("Cache", cacheDetail.GetProperty("name").GetString());
        Assert.Equal("Degraded", cacheDetail.GetProperty("status").GetString());
        Assert.Equal("Cache is slow", cacheDetail.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task ResponseWriter_Uses_CamelCase_Property_Names()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry> { ["Test"] = new(HealthStatus.Healthy, "Test", TimeSpan.Zero, null, null) },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);

        Assert.Contains("\"status\":", response);
        Assert.Contains("\"reason\":", response);
        Assert.Contains("\"details\":", response);
        Assert.Contains("\"name\":", response);
    }

    [Fact]
    public async Task ResponseWriter_Serializes_Enum_As_String()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry> { ["Test"] = new(HealthStatus.Degraded, "Test", TimeSpan.Zero, null, null) },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);

        Assert.Contains("\"Degraded\"", response);
        Assert.DoesNotContain("\"1\"", response);
    }

    [Fact]
    public async Task ResponseWriter_With_Null_Description_Handles_Gracefully()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry> { ["Test"] = new(HealthStatus.Healthy, null, TimeSpan.Zero, null, null) },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        Assert.Equal("Healthy", result.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ResponseWriter_Excludes_Healthy_Entries_From_Reason()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Database"] = new(HealthStatus.Healthy, "Database is healthy", TimeSpan.Zero, null, null),
                ["Cache"] = new(HealthStatus.Unhealthy, "Cache failed", TimeSpan.Zero, null, null),
            },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);
        var reason = result.GetProperty("reason").GetString();

        Assert.Equal("Cache failed", reason);
        Assert.DoesNotContain("Database is healthy", reason);
    }

    [Fact]
    public async Task ResponseWriter_Includes_All_Entries()
    {
        var context = CreateHttpContext();
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["Alpha"] = new(HealthStatus.Healthy, "Alpha service", TimeSpan.Zero, null, null),
            ["Beta"] = new(HealthStatus.Healthy, "Beta service", TimeSpan.Zero, null, null),
            ["Gamma"] = new(HealthStatus.Healthy, "Gamma service", TimeSpan.Zero, null, null),
        };
        var healthReport = new HealthReport(entries, TimeSpan.FromSeconds(1));

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);
        var details = result.GetProperty("details");

        Assert.Equal(3, details.GetArrayLength());

        var entryNames = new List<string>();
        foreach (var detail in details.EnumerateArray())
        {
            entryNames.Add(detail.GetProperty("name").GetString()!);
        }

        Assert.Contains("Alpha", entryNames);
        Assert.Contains("Beta", entryNames);
        Assert.Contains("Gamma", entryNames);
    }

    [Fact]
    public async Task ResponseWriter_With_Degraded_And_Unhealthy_Returns_Unhealthy()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Service1"] = new(HealthStatus.Degraded, "Service1 slow", TimeSpan.Zero, null, null),
                ["Service2"] = new(HealthStatus.Unhealthy, "Service2 failed", TimeSpan.Zero, null, null),
            },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        Assert.Equal("Unhealthy", result.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ResponseWriter_Separates_Multiple_Reasons_With_Semicolon()
    {
        var context = CreateHttpContext();
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Database"] = new(HealthStatus.Unhealthy, "Connection timeout", TimeSpan.Zero, null, null),
                ["Cache"] = new(HealthStatus.Degraded, "High latency", TimeSpan.Zero, null, null),
            },
            TimeSpan.FromSeconds(1)
        );

        await OneGroundHealthChecksResponseWriter.ResponseWriter(context, healthReport);

        var response = GetResponseAsString(context);
        var result = JsonSerializer.Deserialize<JsonElement>(response);
        var reason = result.GetProperty("reason").GetString();

        Assert.Equal("Connection timeout; High latency", reason);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string GetResponseAsString(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
