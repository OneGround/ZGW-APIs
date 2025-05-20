using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;

namespace OneGround.ZGW.Zaken.Web.BusinessRules;

public class ClosedZaakModificationBusinessRule : IClosedZaakModificationBusinessRule
{
    private readonly AuthorizationContext _authorizationContext;

    public ClosedZaakModificationBusinessRule(IAuthorizationContextAccessor authorizatioContextAccessor)
    {
        _authorizationContext = authorizatioContextAccessor.AuthorizationContext;
    }

    public bool ValidateClosedZaakModificationRule(Zaak zaak, List<ValidationError> errors)
    {
        if (zaak.Einddatum.HasValue && !_authorizationContext.IsAuthorized(zaak, AuthorizationScopes.Zaken.ForcedUpdate))
        {
            var error = new ValidationError("zaak", ErrorCode.PermissionDenied, $"Insufficient user rights to modify closed zaak: {zaak.Id}");
            errors.Add(error);
        }

        return errors.Count == 0;
    }
}
