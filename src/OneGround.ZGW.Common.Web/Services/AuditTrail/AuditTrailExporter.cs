using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

#if DEBUG

// Note: This only for testing purposes at this moment) and is not used in Production.
public interface IAuditTrailExporter
{
    Task ExportAsync(Guid hoofdobjectId, bool legacy, CancellationToken cancellationToken);
}

public class AuditTrailExporter : IAuditTrailExporter
{
    private readonly IAuditTrailFactory _auditTrailFactory;

    public AuditTrailExporter(IAuditTrailFactory auditTrailFactory)
    {
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task ExportAsync(Guid hoofdobjectId, bool legacy, CancellationToken cancellationToken)
    {
        using var auditService = _auditTrailFactory.Create(legacy);

        var tempPath = Path.GetTempPath();
        var fullPath = Path.Combine(tempPath, $"audit_{hoofdobjectId}_{DateTime.Now:yyyyMMddHHmmss.fff}.json");

        using var sw = File.CreateText(fullPath);

        sw.WriteLine($"// Note: Export of '{auditService.GetType().Name}'.");

        foreach (var audit in await auditService.GetAuditTrailEntriesAsync(hoofdobjectId))
        {
            var auditLine = new JsonObject
            {
                ["hoofdobjectId"] = hoofdobjectId,
                ["bron"] = audit.Bron,
                ["applicatieId"] = audit.ApplicatieId,
                ["applicatieWeergave"] = audit.ApplicatieWeergave,
                ["actie"] = audit.Actie,
                ["actieWeergave"] = audit.ActieWeergave,
                ["resultaat"] = audit.Resultaat,
                ["hoofdObject"] = audit.HoofdObject,
                ["resource"] = audit.Resource,
                ["resourceUrl"] = audit.ResourceUrl,
                ["toelichting"] = audit.Toelichting,
                ["aanmaakDatum"] = audit.AanmaakDatum,
                ["oud"] = audit.Oud != null ? JsonSerializer.Deserialize<JsonObject>(audit.Oud) : null,
                ["nieuw"] = audit.Nieuw != null ? JsonSerializer.Deserialize<JsonObject>(audit.Nieuw) : null,
                ["requestId"] = audit.RequestId,
            };
            sw.WriteLine(auditLine);
        }
    }
}

#endif
