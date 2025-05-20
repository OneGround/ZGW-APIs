using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.BusinessRules;

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
