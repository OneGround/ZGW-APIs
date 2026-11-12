using FluentValidation;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Helpers;

namespace OneGround.ZGW.Zaken.Web.Validators;

public static class ZaakUrlRuleBuilderExtensions
{
    /// <summary>
    /// Validates that a (non-empty) value is the url of a zaak, i.e. contains the id of that zaak.
    /// </summary>
    public static IRuleBuilderOptions<T, string> ContainsZaakId<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || UriHelper.TryGetResourceId(value, "zaken") != null)
            .WithMessage("De URL verwijst niet naar een zaak.")
            .WithErrorCode(ErrorCode.InvalidResource);
    }
}
