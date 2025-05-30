using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Requests;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1._3;

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

        CascadeRuleFor(r => r.ProcesObjectAard).MaximumLength(200);
        CascadeRuleFor(z => z.BeginGeldigheid).IsDate(false);
        CascadeRuleFor(z => z.EindeGeldigheid).IsDate(false);
        CascadeRuleFor(z => z.BeginObject).IsDate(false);
        CascadeRuleFor(z => z.EindeObject).IsDate(false);
        CascadeRuleFor(z => z.ProcesTermijn).IsDuration();
        CascadeRuleFor(z => z.BesluitTypen).NotNull().IsDistinct();
        CascadeRuleForEach(z => z.BesluitTypen).NotNull().MaximumLength(80);
        CascadeRuleFor(z => z.InformatieObjectTypen).IsDistinct();
        CascadeRuleForEach(z => z.InformatieObjectTypen).IsUri();
    }
}
