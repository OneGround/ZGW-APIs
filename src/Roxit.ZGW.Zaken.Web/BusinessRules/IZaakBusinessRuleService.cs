using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.BusinessRules;

public interface IZaakBusinessRuleService
{
    Task<bool> ValidateAsync(Zaak zaakAdd, string hoodzaakUrl, bool ignoreZaakTypeValidation, List<ValidationError> errors);
    Task<bool> ValidateAsync(Zaak zaakExisting, Zaak zaakUpdate, string hoodzaakUrl, List<ValidationError> errors);
    Task<bool> ValidateZaakDocumentenArchivedStatusAsync(Zaak zaakExisting, List<ValidationError> errors);
}
