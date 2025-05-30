using System.Collections.Generic;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.BusinessRules;

public interface IClosedZaakModificationBusinessRule
{
    bool ValidateClosedZaakModificationRule(Zaak zaak, List<ValidationError> errors);
}
