using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Common.Contracts.v1;

namespace OneGround.ZGW.Besluiten.Web.BusinessRules;

public interface IBesluitBusinessRuleService
{
    Task<bool> ValidateAsync(Besluit besluitAdd, bool ignoreBesluitTypeValidation, bool ignoreZaakValidation, List<ValidationError> errors);
    Task<bool> ValidateAsync(Besluit besluitExisting, Besluit besluitUpdate, bool ignoreZaakValidation, List<ValidationError> errors);
}
