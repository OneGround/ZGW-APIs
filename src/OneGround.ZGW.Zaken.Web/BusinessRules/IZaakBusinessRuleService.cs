using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.BusinessRules;

public interface IZaakBusinessRuleService
{
    Task<bool> ValidateAsync(
        Zaak zaakAdd,
        string hoodzaakUrl,
        IEnumerable<string> relevanteAndereZaakUrls,
        bool ignoreZaakTypeValidation,
        List<ValidationError> errors
    );
    Task<bool> ValidateAsync(
        Zaak zaakExisting,
        Zaak zaakUpdate,
        string hoodzaakUrl,
        IEnumerable<string> relevanteAndereZaakUrls,
        List<ValidationError> errors
    );
    Task<bool> ValidateZaakDocumentenArchivedStatusAsync(Zaak zaakExisting, List<ValidationError> errors);
}
