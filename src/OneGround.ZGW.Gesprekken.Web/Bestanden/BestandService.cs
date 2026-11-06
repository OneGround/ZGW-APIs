using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.BusinessRules;

namespace OneGround.ZGW.Gesprekken.Web.Bestanden;

public interface IBestandService
{
    /// <summary>
    /// Brings the status of a bestand up to date with the storage. The upload goes past this API, so the status is
    /// settled when someone asks for it. The caller saves the changes.
    /// </summary>
    Task VernieuwStatusAsync(Bestand bestand, CancellationToken cancellationToken);

    /// <summary>
    /// Checks that every bijlage refers to an available, unused bestand of the tenant and copies its display fields.
    /// Returns the errors, if any.
    /// </summary>
    Task<List<ValidationError>> KoppelAsync(string rsin, IReadOnlyList<Bijlage> bijlagen, string pad, CancellationToken cancellationToken);
}

public class BestandService : IBestandService
{
    private readonly GrcDbContext _context;
    private readonly IBestandOpslag _opslag;

    public BestandService(GrcDbContext context, IBestandOpslag opslag)
    {
        _context = context;
        _opslag = opslag;
    }

    public async Task VernieuwStatusAsync(Bestand bestand, CancellationToken cancellationToken)
    {
        if (bestand.Status != BestandStatus.uploadInAfwachting)
        {
            return;
        }

        var omvang = await _opslag.GetOmvangAsync(bestand.Sleutel, cancellationToken);
        if (omvang == null)
        {
            if (DateTime.UtcNow > bestand.VerlooptOp)
            {
                bestand.Status = BestandStatus.verlopen;
            }

            return;
        }

        if (omvang != bestand.Bestandsomvang)
        {
            bestand.Status = BestandStatus.geweigerd;
            await _opslag.VerwijderAsync(bestand.Sleutel, cancellationToken);
            return;
        }

        bestand.Status = BestandStatus.beschikbaar;
        bestand.GeuploadOp = DateTime.UtcNow;
    }

    public async Task<List<ValidationError>> KoppelAsync(
        string rsin,
        IReadOnlyList<Bijlage> bijlagen,
        string pad,
        CancellationToken cancellationToken
    )
    {
        var errors = new List<ValidationError>();

        for (var i = 0; i < bijlagen.Count; i++)
        {
            var bijlage = bijlagen[i];
            var veld = $"{pad}.{i}.bestand";

            var bestand = await _context.Bestanden.SingleOrDefaultAsync(b => b.Id == bijlage.BestandId && b.Owner == rsin, cancellationToken);
            if (bestand == null)
            {
                errors.Add(new ValidationError(veld, ErrorCode.NotFound, "Het bestand bestaat niet."));
                continue;
            }

            await VernieuwStatusAsync(bestand, cancellationToken);
            if (bestand.Status != BestandStatus.beschikbaar)
            {
                errors.Add(new ValidationError(veld, ErrorCodes.BestandNietBeschikbaar, "Het bestand is niet beschikbaar."));
                continue;
            }

            if (await _context.Bijlagen.AnyAsync(b => b.BestandId == bestand.Id, cancellationToken))
            {
                errors.Add(new ValidationError(veld, ErrorCodes.BestandAlGekoppeld, "Het bestand hoort al bij een andere bijdrage."));
                continue;
            }

            bijlage.Titel = bestand.Titel;
            bijlage.Bestandsnaam = bestand.Bestandsnaam;
            bijlage.ContentType = bestand.ContentType;
            bijlage.Bestandsomvang = bestand.Bestandsomvang;
        }

        return errors;
    }
}
