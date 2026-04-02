using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

// Note: This only for testing purposes and is not used in Production.
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
        using var auditService = _auditTrailFactory.Create(new AuditTrailOptions(), legacy);

        // TODO: Fix not using hard-coded path here
        using var sw = File.CreateText($@"c:\temp\audit_{hoofdobjectId}_{DateTime.Now:yyyyMMddHHmms}.json");

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
