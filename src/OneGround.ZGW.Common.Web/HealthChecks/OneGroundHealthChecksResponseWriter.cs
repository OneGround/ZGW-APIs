using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OneGround.ZGW.Common.Web.HealthChecks.Models;

namespace OneGround.ZGW.Common.Web.HealthChecks;

public static class OneGroundHealthChecksResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };

    public static Task ResponseWriter(HttpContext context, HealthReport healthReport)
    {
        var responseModel = new OneGroundHealthChecksResult { Status = HealthStatus.Healthy };
        var reasons = new List<string>();

        foreach (var entry in healthReport.Entries)
        {
            responseModel.Details.Add(
                new OneGroundHealthChecksDetail()
                {
                    Name = entry.Key,
                    Status = entry.Value.Status,
                    Reason = entry.Value.Description,
                }
            );
            if (entry.Value.Status < responseModel.Status)
            {
                responseModel.Status = entry.Value.Status;
            }

            if (entry.Value.Status != HealthStatus.Healthy)
            {
                reasons.Add(entry.Value.Description);
            }
        }

        responseModel.Reason = string.Join("; ", reasons);

        var json = JsonSerializer.Serialize(responseModel, SerializerOptions);

        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(json);
    }
}
