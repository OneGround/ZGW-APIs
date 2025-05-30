using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Common.Contracts.v1;

namespace Roxit.ZGW.Besluiten.Web.BusinessRules;

public interface IBesluitInformatieObjectBusinessRuleService
{
    Task<bool> ValidateAsync(
        Besluit besluit,
        BesluitInformatieObject besluitInformatieObjectAdd,
        bool ignoreInformatieObjectValidation,
        List<ValidationError> errors
    );
}
