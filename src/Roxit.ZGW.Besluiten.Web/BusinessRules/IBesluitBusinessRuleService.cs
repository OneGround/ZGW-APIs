using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Common.Contracts.v1;

namespace Roxit.ZGW.Besluiten.Web.BusinessRules;

public interface IBesluitBusinessRuleService
{
    Task<bool> ValidateAsync(Besluit besluitAdd, bool ignoreBesluitTypeValidation, bool ignoreZaakValidation, List<ValidationError> errors);
    Task<bool> ValidateAsync(Besluit besluitExisting, Besluit besluitUpdate, bool ignoreZaakValidation, List<ValidationError> errors);
}
