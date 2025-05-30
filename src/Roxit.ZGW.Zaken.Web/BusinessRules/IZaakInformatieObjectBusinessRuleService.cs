using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.BusinessRules;

public interface IZaakInformatieObjectBusinessRuleService
{
    Task<bool> ValidateAsync(
        ZaakInformatieObject zaakInformatieObjectAdd,
        string zaakUrl,
        bool ignoreInformatieObjectValidation,
        List<ValidationError> errors
    );
    Task<bool> ValidateAsync(
        ZaakInformatieObject zaakInformatieObjectExisting,
        ZaakInformatieObject zaakInformatieObjectUpdate,
        string zaakUrl,
        List<ValidationError> errors
    );
}
