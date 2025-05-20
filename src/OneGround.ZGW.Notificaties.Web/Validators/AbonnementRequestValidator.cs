using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Notificaties.Contracts.v1.Requests;

namespace OneGround.ZGW.Notificaties.Web.Validators;

public class AbonnementRequestValidator : ZGWValidator<AbonnementRequestDto>
{
    public AbonnementRequestValidator()
    {
        CascadeRuleFor(r => r.CallbackUrl).NotNull().NotEmpty().IsUri().MaximumLength(200);
        CascadeRuleFor(r => r.Auth).NotNull().NotEmpty().MaximumLength(1000);
        CascadeRuleFor(r => r.Kanalen).NotNull().NotEmpty();
        CascadeRuleForEach(r => r.Kanalen)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(r => r.Naam).NotNull().NotEmpty().MaximumLength(50);
                v.CascadeRuleForEach(r => r.Filters)
                    .ChildRules(v =>
                    {
                        v.CascadeRuleFor(r => r.Key).NotNull().NotEmpty().MaximumLength(1000);
                    });
            });
    }

    //private void ValidateKanalen(IList<AbonnementKanaalDto> kanalen, IList<ValidationError> errors)
    //{
    //    int ki = 1;
    //    foreach (var kanaal in kanalen)
    //    {
    //        new FieldValidator($"kanaal[{ki}].naam", kanaal.Naam, errors)
    //            .ValidateRequired()
    //            .ValidateLength(50);

    //        if (kanaal.Filters != null)
    //        {
    //            int fi = 1;
    //            foreach (var filter in kanaal.Filters)
    //            {
    //                new FieldValidator($"kanaal[{ki}].filters[{fi}].key", filter.Key, errors)
    //                    .ValidateRequired()
    //                    .ValidateLength(200);

    //                fi++;
    //            }
    //        }
    //        ki++;
    //    }
    //}
}
