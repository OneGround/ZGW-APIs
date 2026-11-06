using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Contracts.v1;

namespace OneGround.ZGW.Gesprekken.Web.BusinessRules;

public interface IGesprekBusinessRuleService
{
    /// <summary>
    /// A new gesprek needs an existing zaak as aanleiding that has not been closed (no einddatum).
    /// </summary>
    Task<bool> ValidateAanleidingZaakAsync(string aanleidingZaak, List<ValidationError> errors);
}
