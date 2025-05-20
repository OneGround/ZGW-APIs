using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Common.Contracts.v1;

namespace OneGround.ZGW.Besluiten.Web.BusinessRules;

public interface IBesluitInformatieObjectBusinessRuleService
{
    Task<bool> ValidateAsync(
        Besluit besluit,
        BesluitInformatieObject besluitInformatieObjectAdd,
        bool ignoreInformatieObjectValidation,
        List<ValidationError> errors
    );
}
