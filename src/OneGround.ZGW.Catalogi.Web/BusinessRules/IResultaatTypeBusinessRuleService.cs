using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Referentielijsten.Contracts.v1;

namespace OneGround.ZGW.Catalogi.Web.BusinessRules;

public interface IResultaatTypeBusinessRuleService
{
    Task<bool> ValidateAsync(ResultaatType resultType, ZaakType zaakType, ResultaatDto resultaat, List<ValidationError> errors);

    Task<bool> ValidateUpdateAsync(ResultaatType newResultaatType, ZaakType zaakType, ResultaatDto resultaat, List<ValidationError> errors);
}
