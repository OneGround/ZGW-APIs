using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Referentielijsten.Contracts.v1;

namespace Roxit.ZGW.Catalogi.Web.BusinessRules;

public interface IResultaatTypeBusinessRuleService
{
    Task<bool> ValidateAsync(ResultaatType resultType, ZaakType zaakType, ResultaatDto resultaat, List<ValidationError> errors);

    Task<bool> ValidateUpdateAsync(ResultaatType newResultaatType, ZaakType zaakType, ResultaatDto resultaat, List<ValidationError> errors);
}
