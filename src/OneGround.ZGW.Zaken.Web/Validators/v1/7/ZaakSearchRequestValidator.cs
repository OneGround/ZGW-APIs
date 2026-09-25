using FluentValidation;
using FluentValidation.Results;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Validators.v1._5;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7;

// Note: Unlike the v1._5 ZaakSearchRequestValidator, this does NOT check "expand" against the old
// SupportedExpands whitelist -- for v1.7, expand is validated by the new ExpandValidator<T> in the
// controller instead (see ZakenController.cs GetAllAsync/SearchAsync/GetAsync). This validator only
// covers what FluentValidation still owns: the shared searchable-fields rules, and the "expand"
// and "fields" mutual-exclusivity check (mirrors DRC 1.7's EnkelvoudigInformatieObjectSearchRequestValidator).
public class ZaakSearchRequestValidator : ZGWValidator<ZaakSearchRequestDto>
{
    public ZaakSearchRequestValidator()
    {
        CascadeRuleFor(p => p)
            .Custom(
                (request, context) =>
                {
                    if (!string.IsNullOrWhiteSpace(request.Expand) && request.Fields is not null)
                    {
                        context.AddFailure(
                            new ValidationFailure("fields", "Je mag niet zowel 'expand' als 'fields' opgeven in dezelfde aanvraag.")
                            {
                                ErrorCode = ErrorCode.Invalid,
                            }
                        );
                    }
                }
            );

        // Add validation for the common search fields
        Include(new ZakenCommonSearchableFields());

        CascadeRuleFor(r => r.ZaakGeometry)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(z => z.Within).NotNull();
            });

        // Note: The "<*>__in" search fields are array types
        CascadeRuleForEach(p => p.Archiefnominatie__in).IsEnumName(typeof(ArchiefNominatie)).WithName("archiefnominatie__in");
        CascadeRuleForEach(p => p.Archiefstatus__in).IsEnumName(typeof(ArchiefStatus)).WithName("archiefstatus__in");
        CascadeRuleForEach(p => p.Bronorganisatie__in).IsRsin().WithName("bronorganisatie__in");

        CascadeRuleForEach(p => p.Zaaktype__in).IsUri().WithName("zaaktype__in");
        CascadeRuleForEach(p => p.Uuid__in).IsGuid().WithName("uuid__in");
    }
}
