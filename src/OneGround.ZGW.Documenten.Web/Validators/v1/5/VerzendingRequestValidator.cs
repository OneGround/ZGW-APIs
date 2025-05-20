using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._5;

public class VerzendingRequestValidator : ZGWValidator<VerzendingRequestDto>
{
    public VerzendingRequestValidator()
    {
        CascadeRuleFor(v => v.Betrokkene).NotNull().NotEmpty().MaximumLength(200).IsUri();
        CascadeRuleFor(v => v.InformatieObject).NotNull().NotEmpty().MaximumLength(1000).IsUri();
        CascadeRuleFor(v => v.AardRelatie).IsEnumName(typeof(AardRelatie));
        CascadeRuleFor(v => v.Verzenddatum).IsDate(required: false);
        CascadeRuleFor(v => v.OntvangstDatum).IsDate(required: false);
        CascadeRuleFor(v => v.Toelichting).MaximumLength(200);
        CascadeRuleFor(v => v.Contactpersoon).NotNull().NotEmpty().MaximumLength(1000);
        CascadeRuleFor(v => v.ContactpersoonNaam).MaximumLength(40);
        CascadeRuleFor(v => v.Faxnummer).MaximumLength(15);
        CascadeRuleFor(v => v.EmailAdres).MaximumLength(100);
        CascadeRuleFor(v => v.Telefoonnummer).MaximumLength(15);

        When(
            v => v.BinnenlandsCorrespondentieAdres != null,
            () =>
            {
                string parentField = "binnenlandsCorrespondentieAdres";

                CascadeRuleFor(v => v.BinnenlandsCorrespondentieAdres.Huisletter).MaximumLength(1).OverridePropertyName($"{parentField}.huisletter");
                CascadeRuleFor(v => v.BinnenlandsCorrespondentieAdres.Huisnummer)
                    .IsInRange(1, 99999)
                    .OverridePropertyName($"{parentField}.huisnummer");
                CascadeRuleFor(v => v.BinnenlandsCorrespondentieAdres.HuisnummerToevoeging)
                    .MaximumLength(4)
                    .OverridePropertyName($"{parentField}.huisnummerToevoeging");
                CascadeRuleFor(v => v.BinnenlandsCorrespondentieAdres.NaamOpenbareRuimte)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(80)
                    .OverridePropertyName($"{parentField}.naamOpenbareRuimte");
                CascadeRuleFor(v => v.BinnenlandsCorrespondentieAdres.Postcode).MaximumLength(6).OverridePropertyName($"{parentField}.postcode");
                CascadeRuleFor(v => v.BinnenlandsCorrespondentieAdres.WoonplaatsNaam)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(80)
                    .OverridePropertyName($"{parentField}.woonplaatsNaam");
            }
        );

        When(
            v => v.BuitenlandsCorrespondentieAdres != null,
            () =>
            {
                string parentField = "buitenlandsCorrespondentieAdres";

                CascadeRuleFor(v => v.BuitenlandsCorrespondentieAdres.AdresBuitenland1)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(35)
                    .OverridePropertyName($"{parentField}.adresBuitenland1");
                CascadeRuleFor(v => v.BuitenlandsCorrespondentieAdres.AdresBuitenland2)
                    .MaximumLength(35)
                    .OverridePropertyName($"{parentField}.adresBuitenland2");
                CascadeRuleFor(v => v.BuitenlandsCorrespondentieAdres.AdresBuitenland3)
                    .MaximumLength(35)
                    .OverridePropertyName($"{parentField}.adresBuitenland3");
                CascadeRuleFor(v => v.BuitenlandsCorrespondentieAdres.LandPostadres)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(200)
                    .OverridePropertyName($"{parentField}.landPostadres");
            }
        );

        When(
            v => v.CorrespondentiePostadres != null,
            () =>
            {
                string parentField = "correspondentiePostadres";

                CascadeRuleFor(v => v.CorrespondentiePostadres.PostbusOfAntwoordnummer)
                    .IsInRange(1, 9999)
                    .OverridePropertyName($"{parentField}.postBusOfAntwoordnummer");
                CascadeRuleFor(v => v.CorrespondentiePostadres.PostadresPostcode)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(6)
                    .OverridePropertyName($"{parentField}.postadresPostcode");
                CascadeRuleFor(v => v.CorrespondentiePostadres.PostadresType)
                    .IsEnumName(typeof(PostadresType))
                    .OverridePropertyName($"{parentField}.postadresType");
                CascadeRuleFor(v => v.CorrespondentiePostadres.WoonplaatsNaam)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(80)
                    .OverridePropertyName($"{parentField}.woonplaatsNaam");
            }
        );
    }
}
