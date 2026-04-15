using System;
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
            bool result = await ResolveSnapshotOrDeltaAsync(audit.HoofdObjectId.Value, actie, audit.Nieuw, audit.Oud, delta, cancellationToken);
            if (!result)
            {
                // No changes → Do not log
                return;
            }
        }

        await _context.AuditTrailDeltas.AddAsync(delta, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
