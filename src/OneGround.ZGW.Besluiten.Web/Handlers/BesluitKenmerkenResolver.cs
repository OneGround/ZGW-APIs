using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Web.Kenmerken;

namespace OneGround.ZGW.Besluiten.Web.Handlers;

public interface IBesluitKenmerkenResolver
{
    Task<Dictionary<string, string>> GetKenmerkenAsync(Besluit besluit, CancellationToken cancellationToken);
}

public class BesluitKenmerkenResolver : BaseKenmerkenResolver, IBesluitKenmerkenResolver
{
    private readonly ILogger<BesluitKenmerkenResolver> _logger;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;

    public BesluitKenmerkenResolver(ILogger<BesluitKenmerkenResolver> logger, ICatalogiServiceAgent catalogiServiceAgent)
    {
        _logger = logger;
        _catalogiServiceAgent = catalogiServiceAgent;
    }

    public async Task<Dictionary<string, string>> GetKenmerkenAsync(Besluit besluit, CancellationToken cancellationToken)
    {
        return new Dictionary<string, string>
        {
            { Constants.BrcBesluittype, besluit.BesluitType },
            { Constants.BrcVerantwoordelijkeOrganisatie, besluit.VerantwoordelijkeOrganisatie },
            { Constants.BrcBesluittypeOmschrijving, await GetBesluitTypeOmschrijvingFromBesluitAsync(besluit) },
            { Constants.BrcCatalogus, GetCatalogusUrlFromResource(besluit.BesluitType, besluit.CatalogusId) },
            { Constants.BrcDomein, await GetDomeinFromBesluitAsync(besluit) },
        };
    }

    private async Task<string> GetBesluitTypeOmschrijvingFromBesluitAsync(Besluit besluit)
    {
        var result = await _catalogiServiceAgent.GetBesluitTypeByUrlAsync(besluit.BesluitType);
        if (!result.Success)
        {
            _logger.LogWarning("Could not retrieve besluittype-omschrijving '{BesluitType}'.", besluit.BesluitType);
            return "(Besluittype-omschrijving not determined)";
        }
        return result.Response.Omschrijving;
    }

    private async Task<string> GetDomeinFromBesluitAsync(Besluit besluit)
    {
        var catalogusUri = GetCatalogusUrlFromResource(besluit.BesluitType, besluit.CatalogusId);

        var result = await _catalogiServiceAgent.GetCatalogusAsync(catalogusUri);
        if (!result.Success)
        {
            _logger.LogWarning("Could not retrieve catalogus '{catalogusUri}'.", catalogusUri);
            return "(Domein not determined)";
        }
        return result.Response.Domein;
    }
}
