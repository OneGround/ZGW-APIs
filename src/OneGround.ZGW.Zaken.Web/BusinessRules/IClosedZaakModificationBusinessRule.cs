using System.Collections.Generic;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.BusinessRules;

public interface IClosedZaakModificationBusinessRule
{
    bool ValidateClosedZaakModificationRule(Zaak zaak, List<ValidationError> errors);
}
