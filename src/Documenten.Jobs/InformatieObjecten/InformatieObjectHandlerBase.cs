using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Documenten.ServiceAgent.v1;

namespace Roxit.ZGW.Documenten.Jobs.InformatieObjecten;

public abstract class InformatieObjectHandlerBase<TLogger>
{
    protected readonly ILogger<TLogger> _logger;
    protected readonly IDocumentenServiceAgent _documentenServiceAgent;
    protected readonly IOrganisationContextFactory _organisationContextFactory;

    protected InformatieObjectHandlerBase(
        ILogger<TLogger> logger,
        IDocumentenServiceAgent documentenServiceAgent,
        IOrganisationContextFactory organisationContextFactory
    )
    {
        _logger = logger;
        _documentenServiceAgent = documentenServiceAgent;
        _organisationContextFactory = organisationContextFactory;
    }

    protected (string objecttype, string informatieobject) GetInformatieObject(KeyValuePair<string, string> informatieObjectKenmerk)
    {
        if (string.IsNullOrEmpty(informatieObjectKenmerk.Value))
            throw new InvalidOperationException($"InformatieObject kenmerk value is null or empty for key: {informatieObjectKenmerk.Key}");
        return informatieObjectKenmerk.Key switch
        {
            "zaakinformatieobject.informatieobject" => ("zaak", informatieObjectKenmerk.Value),
            "besluitinformatieobject.informatieobject" => ("besluit", informatieObjectKenmerk.Value),
            _ => throw new InvalidOperationException($"Unknown InformatieObject kenmerk key: {informatieObjectKenmerk.Key}"),
        };
    }

    protected IDisposable GetLoggingScope(string rsin, Guid correlationId)
    {
        return _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["RSIN"] = rsin });
    }
}
