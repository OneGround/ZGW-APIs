using System;
using System.Linq;
using System.Threading.Tasks;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web.Authorization;

namespace Roxit.ZGW.Autorisaties.ServiceAgent;

public sealed class AutorisatiesServiceAgentAuthorizationResolver : IAuthorizationResolver
{
    private readonly IAutorisatiesServiceAgent _authorisatiesServiceAgent;

    public AutorisatiesServiceAgentAuthorizationResolver(IAutorisatiesServiceAgent authorisatiesServiceAgent)
    {
        _authorisatiesServiceAgent = authorisatiesServiceAgent;
    }

    public async Task<AuthorizedApplication> ResolveAsync(string clientId, string component, string[] scopes)
    {
        var result = await _authorisatiesServiceAgent.GetApplicatieByClientIdAsync(clientId);

        if (!result.Success || result.Response == null)
            return null;

        var application = result.Response;

        return new AuthorizedApplication
        {
            Label = application.Label,
            HasAllAuthorizations = application.HeeftAlleAutorisaties,
            Authorizations = application
                .Autorisaties.Where(a => a.Component.Equals(component, StringComparison.OrdinalIgnoreCase))
                .Where(a => a.Scopes.Any(s => scopes.Contains(s)))
                .Select(a =>
                {
                    int? maximumVertrouwelijkheidAanduiding = default;
                    if (Enum.TryParse<VertrouwelijkheidAanduiding>(a.MaxVertrouwelijkheidaanduiding, out var value))
                    {
                        maximumVertrouwelijkheidAanduiding = (int)value;
                    }
                    return new AuthorizationPermission
                    {
                        MaximumVertrouwelijkheidAanduiding = maximumVertrouwelijkheidAanduiding,
                        InformatieObjectType = a.InformatieObjectType,
                        ZaakType = a.ZaakType,
                        BesluitType = a.BesluitType,
                        Scopes = a.Scopes,
                    };
                }),
        };
    }
}
