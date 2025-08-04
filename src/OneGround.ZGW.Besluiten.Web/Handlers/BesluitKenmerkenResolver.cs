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
            { "besluittype", besluit.BesluitType },
            { "verantwoordelijke_organisatie", besluit.VerantwoordelijkeOrganisatie },
            // Note: New fields that can be filtered on
            { "besluittype_omschrijving", await GetBesluitTypeOmschrijvingFromBesluitAsync(besluit) },
            { "catalogus", GetCatalogusUrlFromResource(besluit.BesluitType, besluit.CatalogusId) },
            { "domein", await GetDomeinFromBesluitAsync(besluit) },
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
