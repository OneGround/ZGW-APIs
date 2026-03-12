using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Web.Kenmerken;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Handlers;

public interface IDocumentKenmerkenResolver
{
    Task<Dictionary<string, string>> GetKenmerkenAsync(EnkelvoudigInformatieObject informatieobject, CancellationToken cancellationToken);
}

public class DocumentKenmerkenResolver : BaseKenmerkenResolver, IDocumentKenmerkenResolver
{
    private readonly ILogger<DocumentKenmerkenResolver> _logger;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;

    public DocumentKenmerkenResolver(ILogger<DocumentKenmerkenResolver> logger, ICatalogiServiceAgent catalogiServiceAgent)
    {
        _logger = logger;
        _catalogiServiceAgent = catalogiServiceAgent;
    }

    public async Task<Dictionary<string, string>> GetKenmerkenAsync(EnkelvoudigInformatieObject informatieobject, CancellationToken cancellationToken)
    {
        var latestEnkelvoudigInformatieObjectVersie = informatieobject.LatestEnkelvoudigInformatieObjectVersie;

        return new Dictionary<string, string>
        {
            { Constants.DrcBronorganisatie, latestEnkelvoudigInformatieObjectVersie.Bronorganisatie },
            { Constants.DrcInformatieobjecttype, informatieobject.InformatieObjectType },
            {
                Constants.DrcVertrouwelijkheidaanduiding,
                latestEnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding.HasValue
                    ? $"{latestEnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding}"
                    : null
            },
            { Constants.DrcInformatieobjecttypeOmschrijving, await GetInformatieObjectTypeOmschrijvingFromInformatieObjectAsync(informatieobject) },
            { Constants.DrcCatalogus, GetCatalogusUrlFromResource(informatieobject.InformatieObjectType, informatieobject.CatalogusId) },
            { Constants.DrcDomein, await GetDomeinFromInformatieObjectAsync(informatieobject) },
            { Constants.DrcStatus, latestEnkelvoudigInformatieObjectVersie.Status.ToString() },
            { Constants.DrcInhoudIsVervallen, latestEnkelvoudigInformatieObjectVersie.InhoudIsVervallen.ToString() },
        };
    }

    private async Task<string> GetInformatieObjectTypeOmschrijvingFromInformatieObjectAsync(EnkelvoudigInformatieObject informatieobject)
    {
        var result = await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(informatieobject.InformatieObjectType);
        if (!result.Success)
        {
            _logger.LogWarning(
                "Could not retrieve informatieobjecttype-omschrijving '{InformatieObjectType}'.",
                informatieobject.InformatieObjectType
            );
            return "(Informatieobjecttype-omschrijving not determined)";
        }
        return result.Response.Omschrijving;
    }

    private async Task<string> GetDomeinFromInformatieObjectAsync(EnkelvoudigInformatieObject informatieobject)
    {
        var catalogusUri = GetCatalogusUrlFromResource(informatieobject.InformatieObjectType, informatieobject.CatalogusId);

        var result = await _catalogiServiceAgent.GetCatalogusAsync(catalogusUri);
        if (!result.Success)
        {
            _logger.LogWarning("Could not retrieve catalogus '{catalogusUri}'.", catalogusUri);
            return "(Domein not determined)";
        }
        return result.Response.Domein;
    }
}
