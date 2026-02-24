using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Web.Kenmerken;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;

namespace OneGround.ZGW.Zaken.Web.Handlers;

public interface IZaakKenmerkenResolver
{
    Task<Dictionary<string, string>> GetKenmerkenAsync(Zaak zaak, CancellationToken cancellationToken);
}

public class ZaakKenmerkenResolver : BaseKenmerkenResolver, IZaakKenmerkenResolver
{
    private readonly ILogger<ZaakKenmerkenResolver> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;

    public ZaakKenmerkenResolver(ILogger<ZaakKenmerkenResolver> logger, IServiceProvider serviceProvider, ICatalogiServiceAgent catalogiServiceAgent)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _catalogiServiceAgent = catalogiServiceAgent;
    }

    public async Task<Dictionary<string, string>> GetKenmerkenAsync(Zaak zaak, CancellationToken cancellationToken)
    {
        var kenmerken = new Dictionary<string, string>
        {
            { "bronorganisatie", zaak.Bronorganisatie },
            { "zaaktype", zaak.Zaaktype },
            { "vertrouwelijkheidaanduiding", zaak.VertrouwelijkheidAanduiding.ToString() },
            // Note: New fields that can be filtered on
            { "zaaktype_identificatie", await GetZaakTypeIdentificatieFromZaakAsync(zaak) },
            { "archiefstatus", zaak.Archiefstatus.ToString() },
            { "archiefnominatie", zaak.Archiefnominatie.ToString() },
            { "opdrachtgevende_organisatie", zaak.OpdrachtgevendeOrganisatie },
            { "catalogus", GetCatalogusUrlFromResource(zaak.Zaaktype, zaak.CatalogusId) },
            { "domein", await GetDomeinFromZaakAsync(zaak) },
            { "is_eindzaakstatus", await IsEindZaakStatusAsync(zaak, cancellationToken) }, // Note: "False" or "True"
        };

        if (zaak.Kenmerken.Count() > 0)
        {
            for (int i = 0; i < zaak.Kenmerken.Count; i++)
            {
                kenmerken.Add($"kenmerk_bron|{i + 1}", zaak.Kenmerken[i].Bron);
                kenmerken.Add($"kenmerk_waarde|{i + 1}", zaak.Kenmerken[i].Kenmerk);
            }
        }
        return kenmerken;
    }

    private async Task<string> IsEindZaakStatusAsync(Zaak zaak, CancellationToken cancellationToken)
    {
        var zaakstatussen = zaak.ZaakStatussen;
        if (zaakstatussen == null)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

            zaakstatussen = await context.ZaakStatussen.Where(s => s.ZaakId == zaak.Id).ToListAsync(cancellationToken);
        }

        var latestStatusType = zaakstatussen.MaxBy(s => s.DatumStatusGezet);
        if (latestStatusType == null)
        {
            return false.ToString();
        }

        var result = await _catalogiServiceAgent.GetStatusTypeByUrlAsync(latestStatusType.StatusType);
        if (!result.Success)
        {
            _logger.LogWarning("Could not retrieve status-type '{Url}' from catalogi API.", latestStatusType.Url);
            return false.ToString();
        }

        return result.Response.IsEindStatus.ToString();
    }

    private async Task<string> GetZaakTypeIdentificatieFromZaakAsync(Zaak zaak)
    {
        var result = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaak.Zaaktype);
        if (!result.Success)
        {
            _logger.LogWarning("Could not retrieve zaaktype-identificatie '{Zaaktype}'.", zaak.Zaaktype);
            return "(Zaaktype-identificatie not determined)";
        }
        return result.Response.Identificatie;
    }

    private async Task<string> GetDomeinFromZaakAsync(Zaak zaak)
    {
        var catalogusUri = GetCatalogusUrlFromResource(zaak.Zaaktype, zaak.CatalogusId);

        var result = await _catalogiServiceAgent.GetCatalogusAsync(catalogusUri);
        if (!result.Success)
        {
            _logger.LogWarning("Could not retrieve catalogus '{catalogusUri}'.", catalogusUri);
            return "(Domein not determined)";
        }
        return result.Response.Domein;
    }
}
