using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess.AuditTrail;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public class DeltaBasedAuditTrailWithImporter : DeltaBasedAuditTrail, IDeltaBasedAuditTrailWithImporter
{
    public DeltaBasedAuditTrailWithImporter(
        IDbContextWithAuditTrail context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IEntityUriService uriService
    )
        : base(context, mapper, httpContextAccessor, uriService) { }

    public override string Name => "Deltas_Importer";

    public async Task ImportAsync(AuditTrailRegel audit, CancellationToken cancellationToken = default)
    {
        var delta = new AuditTrailDelta
        {
            Id = audit.Id,
            AanmaakDatum = audit.AanmaakDatum,
            Actie = audit.Actie,
            ActieWeergave = audit.ActieWeergave,
            ApplicatieId = audit.ApplicatieId,
            ApplicatieWeergave = audit.ApplicatieWeergave,
            Bron = audit.Bron,
            GebruikersId = audit.GebruikersId,
            GebruikersWeergave = audit.GebruikersWeergave,
            HoofdObject = audit.HoofdObject,
            HoofdObjectId = audit.HoofdObjectId,
            Resource = audit.Resource,
            ResourceUrl = audit.ResourceUrl,
            ResourceWeergave = audit.ResourceWeergave,
            RequestId = audit.RequestId,
            Resultaat = audit.Resultaat,
            // TODO: We should register somewhere that it is clear that Migrator does had written this log (so may be add extra column?)
            Toelichting = "Migrator", // = audit.Toelichting,
            // Resolved soon, but for now we need to set these to null to prevent issues with the delta generation logic
            DeltaJson = null,
            SnapshotJson = null,
            Versie = 0,
            ResourceId = _uriService.GetId(audit.ResourceUrl),
        };

        bool shouldCreateSnapshotOrDelta = true;

        if (!Enum.TryParse<AuditActie>(audit.Actie, out var actie))
        {
            shouldCreateSnapshotOrDelta = false;
        }

        if (!audit.HoofdObjectId.HasValue)
        {
            shouldCreateSnapshotOrDelta = false;
        }

        if (shouldCreateSnapshotOrDelta)
        {
            bool result = await ResolveSnapshotOrDeltaAsync(actie, audit.Nieuw, audit.Oud, delta);
            if (!result)
            {
                // No changes → Do not log
                return;
            }

            // TODO: How it was before
            //switch (actie)
            //{
            //    case AuditActie.create:
            //    {
            //        delta.Versie = 1; // await GetNextVersion(audittrail.ResourceId.Value); // TODO: test!!
            //        delta.SnapshotJson = audit.Nieuw;
            //        break;
            //    }

            //    case AuditActie.destroy:
            //    {
            //        delta.Versie = await GetNextVersion(delta.ResourceId.Value);
            //        delta.DeltaJson = audit.Oud;
            //        break;
            //    }

            //    case AuditActie.update:
            //    case AuditActie.partial_update:
            //    {
            //        var original = JsonSerializer.Deserialize<JsonObject>(audit.Oud);
            //        var current = JsonSerializer.Deserialize<JsonObject>(audit.Nieuw);

            //        // Genereer delta
            //        var _delta = AuditDeltaGenerator.GenerateDelta(original, current);

            //        // No changes → Do not log
            //        if (_delta == null || _delta.Count == 0)
            //            return;

            //        var versie = await GetNextVersion(delta.ResourceId.Value);

            //        // Check if this is a snapshot version
            //        bool isSnapshotVersion = versie % _snapshotInterval == 0;

            //        delta.DeltaJson = isSnapshotVersion ? null : _delta.ToJsonString();
            //        delta.SnapshotJson = isSnapshotVersion ? audit.Nieuw : null;
            //        delta.Versie = versie;
            //        break;
            //    }

            //    case AuditActie.retrieve:
            //    {
            //        // No delta for reads, only snapshot
            //        delta.SnapshotJson = audit.Nieuw;
            //        break;
            //    }
            //}
        }

        await _context.AuditTrailDeltas.AddAsync(delta, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
