using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Gesprekken.Web.Configuration;
using OneGround.ZGW.Zaken.ServiceAgent.v1;

namespace OneGround.ZGW.Gesprekken.Web.BusinessRules;

public class GesprekBusinessRuleService : IGesprekBusinessRuleService
{
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public GesprekBusinessRuleService(IZakenServiceAgent zakenServiceAgent, IConfiguration configuration)
    {
        _zakenServiceAgent = zakenServiceAgent;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>() ?? new ApplicationConfiguration();
    }

    public async Task<bool> ValidateAanleidingZaakAsync(string aanleidingZaak, List<ValidationError> errors)
    {
        if (_applicationConfiguration.IgnoreZaakValidation)
            return true;

        var zaak = await _zakenServiceAgent.GetZaakByUrlAsync(aanleidingZaak);
        if (!zaak.Success)
        {
            errors.Add(new ValidationError("aanleidingZaak", zaak.Error.Code, zaak.Error.Title));
            return false;
        }

        if (!string.IsNullOrEmpty(zaak.Response.Einddatum))
        {
            errors.Add(new ValidationError("aanleidingZaak", ErrorCodes.ZaakGesloten, "Bij een gesloten zaak kan geen gesprek worden gestart."));
            return false;
        }

        return true;
    }
}
