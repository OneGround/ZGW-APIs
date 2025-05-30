using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1.Requests;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1;

public class ResultaatTypeRequestDtoValidator : ZGWValidator<ResultaatTypeRequestDto>
{
    public ResultaatTypeRequestDtoValidator()
    {
        CascadeRuleFor(r => r.ZaakType).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(r => r.Omschrijving).NotNull().NotEmpty().MaximumLength(20);
        CascadeRuleFor(r => r.ResultaatTypeOmschrijving).NotNull().NotEmpty().MaximumLength(1000).IsUri();
        CascadeRuleFor(r => r.SelectieLijstKlasse).NotNull().NotEmpty().MaximumLength(1000).IsUri();
        CascadeRuleFor(r => r.ArchiefNominatie).IsEnumName(typeof(ArchiefNominatie)).Unless(r => string.IsNullOrEmpty(r.ArchiefNominatie));
        CascadeRuleFor(r => r.ArchiefActieTermijn).IsDuration();
        CascadeRuleFor(r => r.BronDatumArchiefProcedure)
            .ChildRules(validator =>
            {
                validator.CascadeRuleFor(r => r.Afleidingswijze).NotNull().IsEnumName(typeof(Afleidingswijze));
                validator.CascadeRuleFor(r => r.DatumKenmerk).MaximumLength(80);
                validator.CascadeRuleFor(r => r.ObjectType).IsEnumName(typeof(ObjectType)).Unless(r => string.IsNullOrEmpty(r.ObjectType));
                validator.CascadeRuleFor(r => r.Registratie).MaximumLength(80);
                validator.CascadeRuleFor(r => r.ProcesTermijn).IsDuration();
            });
    }
}
