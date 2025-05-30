using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Referentielijsten.Contracts.v1;

namespace Roxit.ZGW.Catalogi.Web.BusinessRules;

public class ResultaatTypeBusinessRuleService : IResultaatTypeBusinessRuleService
{
    public Task<bool> ValidateAsync(ResultaatType resultType, ZaakType zaakType, ResultaatDto resultaat, List<ValidationError> errors)
    {
        // ztc-002
        if (resultaat.ProcesType != zaakType.SelectielijstProcestype)
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.ProcessTypeMismatch,
                "Selectielijstklasse niet van hetzelfde procestype als Resultaattype.zaaktype.selectielijstProcestype"
            );
            errors.Add(error);
        }

        // if process term is empty value, then all values ​​for the derivation mode (Afleidingswijze)
        // MUST be possible - no need to validate further
        if (resultaat.ProcesTermijn == string.Empty)
        {
            return Task.FromResult(errors.Count != 0);
        }

        // ztc-003
        if (resultaat.ProcesTermijn == "nihil")
        {
            if (resultType.BronDatumArchiefProcedure == null || resultType.BronDatumArchiefProcedure.Afleidingswijze != Afleidingswijze.afgehandeld)
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.InvalidAfleidingswijzeForProcesTermijn,
                    "Afleidingswijze 'afgehandeld' is verplicht bij procestermijn 'Nihil'."
                );
                errors.Add(error);
            }
        }
        else if (resultaat.ProcesTermijn == "ingeschatte_bestaansduur_procesobject")
        {
            if (resultType.BronDatumArchiefProcedure.Afleidingswijze != Afleidingswijze.termijn)
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.InvalidAfleidingswijzeForProcesTermijn,
                    "Afleidingswijze 'termijn' is verplicht bij procestermijn 'ingeschatte_bestaansduur_procesobject'."
                );
                errors.Add(error);
            }
        }

        // ztc-004
        if (resultType.BronDatumArchiefProcedure != null)
        {
            if (
                resultType.BronDatumArchiefProcedure.Afleidingswijze == Afleidingswijze.eigenschap
                || resultType.BronDatumArchiefProcedure.Afleidingswijze == Afleidingswijze.zaakobject
                || resultType.BronDatumArchiefProcedure.Afleidingswijze == Afleidingswijze.ander_datumkenmerk
            )
            {
                if (string.IsNullOrEmpty(resultType.BronDatumArchiefProcedure.DatumKenmerk))
                {
                    var error = new ValidationError(
                        "brondatumArchiefprocedure.datumkenmerk",
                        ErrorCode.Required,
                        "Voor afleidingswijzen 'eigenschap, zaakobject, ander_datumkenmerk' is het veld 'brondatumArchiefprocedure.datumkenmerk' vereist."
                    );
                    errors.Add(error);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(resultType.BronDatumArchiefProcedure.DatumKenmerk))
                {
                    var error = new ValidationError(
                        "brondatumArchiefprocedure.datumkenmerk",
                        ErrorCode.MustBeEmpty,
                        "Voor afleidingswijzen die niet 'eigenschap, zaakobject, ander_datumkenmerk' zijn, moet het veld 'brondatumArchiefprocedure.datumkenmerk' leeg zijn."
                    );
                    errors.Add(error);
                }
            }

            // ztc-005
            if (
                resultType.BronDatumArchiefProcedure.Afleidingswijze == Afleidingswijze.afgehandeld
                || resultType.BronDatumArchiefProcedure.Afleidingswijze == Afleidingswijze.termijn
            )
            {
                if (resultType.BronDatumArchiefProcedure.EindDatumBekend)
                {
                    var error = new ValidationError(
                        "brondatumArchiefprocedure.einddatumBekend",
                        ErrorCode.MustBeEmpty,
                        "Voor afleidingswijzen 'afgehandeld, termijn' moet het veld 'brondatumArchiefprocedure.einddatumBekend' niet aangevinkt zijn."
                    );
                    errors.Add(error);
                }
            }

            // ztc-006
            if (
                resultType.BronDatumArchiefProcedure.Afleidingswijze == Afleidingswijze.zaakobject
                || resultType.BronDatumArchiefProcedure.Afleidingswijze == Afleidingswijze.ander_datumkenmerk
            )
            {
                if (!resultType.BronDatumArchiefProcedure.ObjectType.HasValue)
                {
                    var error = new ValidationError(
                        "brondatumArchiefprocedure.objecttype",
                        ErrorCode.Required,
                        "Voor afleidingswijzen 'zaakobject, ander_datumkenmerk' is het veld 'brondatumArchiefprocedure.objecttype' vereist."
                    );
                    errors.Add(error);
                }
            }
            else
            {
                if (resultType.BronDatumArchiefProcedure.ObjectType.HasValue)
                {
                    var error = new ValidationError(
                        "brondatumArchiefprocedure.objecttype",
                        ErrorCode.MustBeEmpty,
                        "Voor afleidingswijzen die niet 'zaakobject, ander_datumkenmerk' zijn, moet het veld 'brondatumArchiefprocedure.objecttype' van ander_datumkenmerk leeg zijn."
                    );
                    errors.Add(error);
                }
            }

            // ztc-007
            if (resultType.BronDatumArchiefProcedure.Afleidingswijze == Afleidingswijze.ander_datumkenmerk)
            {
                if (string.IsNullOrEmpty(resultType.BronDatumArchiefProcedure.Registratie))
                {
                    var error = new ValidationError(
                        "brondatumArchiefprocedure.registratie",
                        ErrorCode.Required,
                        "Voor afleidingswijze 'ander_datumkenmerk' is het veld 'brondatumArchiefprocedure.registratie' vereist."
                    );
                    errors.Add(error);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(resultType.BronDatumArchiefProcedure.Registratie))
                {
                    var error = new ValidationError(
                        "brondatumArchiefprocedure.registratie",
                        ErrorCode.MustBeEmpty,
                        "Voor afleidingswijze die niet 'ander_datumkenmerk' zijn, moet het veld 'brondatumArchiefprocedure.registratie' leeg zijn."
                    );
                    errors.Add(error);
                }
            }

            // ztc-008
            if (resultType.BronDatumArchiefProcedure.Afleidingswijze == Afleidingswijze.termijn)
            {
                if (resultType.BronDatumArchiefProcedure.ProcesTermijn == null)
                {
                    var error = new ValidationError(
                        "brondatumArchiefprocedure.procestermijn",
                        ErrorCode.Required,
                        "Voor afleidingswijze 'termijn' is het veld 'brondatumArchiefprocedure.procestermijn' vereist."
                    );
                    errors.Add(error);
                }
            }
            else
            {
                if (resultType.BronDatumArchiefProcedure.ProcesTermijn != null)
                {
                    var error = new ValidationError(
                        "brondatumArchiefprocedure.procestermijn",
                        ErrorCode.MustBeEmpty,
                        "Voor afleidingswijze die niet 'termijn' zijn, moet het veld 'brondatumArchiefprocedure.procestermijn' leeg zijn."
                    );
                    errors.Add(error);
                }
            }
        }

        return Task.FromResult(errors.Count != 0);
    }

    public async Task<bool> ValidateUpdateAsync(
        ResultaatType newResultaatType,
        ZaakType zaakType,
        ResultaatDto resultaat,
        List<ValidationError> errors
    )
    {
        return await ValidateAsync(newResultaatType, zaakType, resultaat, errors);
    }
}
