using System;
using System.Collections.Generic;
using System.Linq;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.BusinessRules.v1._5;

public interface IVerzendingBusinessRuleService
{
    bool Validate(EnkelvoudigInformatieObject enkelvoudiginformatieobject, Verzending verzending, List<ValidationError> errors);
    bool Validate(
        EnkelvoudigInformatieObject enkelvoudiginformatieobject,
        Verzending existingVerzending,
        Guid? existingVerzendingId,
        List<ValidationError> errors
    );
}

public class VerzendingBusinessRuleService : IVerzendingBusinessRuleService
{
    public bool Validate(EnkelvoudigInformatieObject informatieobject, Verzending verzending, List<ValidationError> errors)
    {
        return Validate(informatieobject, verzending, existingVerzendingId: null, errors);
    }

    public bool Validate(
        EnkelvoudigInformatieObject informatieobject,
        Verzending existingVerzending,
        Guid? existingVerzendingId,
        List<ValidationError> errors
    )
    {
        //
        // 1. To be sure we have only one type of the seven correspondentie-adresses

        var telefoonnummer = !string.IsNullOrEmpty(existingVerzending.Telefoonnummer);
        var faxnummer = !string.IsNullOrEmpty(existingVerzending.Faxnummer);
        var emailAdres = !string.IsNullOrEmpty(existingVerzending.EmailAdres);
        var mijnOverheid = existingVerzending.MijnOverheid;
        bool isBinnenlandsCorrespondentieAdres = existingVerzending.BinnenlandsCorrespondentieAdres != null;
        bool isBuitenlandsCorrespondentieAdres = existingVerzending.BuitenlandsCorrespondentieAdres != null;
        bool isCorrespondentiePostadres = existingVerzending.CorrespondentiePostadres != null;

        int total = 0;
        if (telefoonnummer)
            total++;
        if (faxnummer)
            total++;
        if (emailAdres)
            total++;
        if (mijnOverheid)
            total++;
        if (isBinnenlandsCorrespondentieAdres)
            total++;
        if (isBuitenlandsCorrespondentieAdres)
            total++;
        if (isCorrespondentiePostadres)
            total++;
        if (total != 1)
        {
            errors.Add(new ValidationError("correspondentieAdres", ErrorCode.Invalid, "Verzending moet precies één correspondentieadres bevatten"));
        }

        //
        // 2. To be sure the Ontvangstdatum or Verzenddatum is filled in correctly depending on AardRelatie

        if (existingVerzending.AardRelatie == AardRelatie.afzender)
        {
            if (!existingVerzending.Ontvangstdatum.HasValue)
            {
                errors.Add(
                    new ValidationError("ontvangstdatum", ErrorCode.Required, $"Veld moet gevuld zijn als aardrelatie '{AardRelatie.afzender}' is.")
                );
            }
            if (existingVerzending.Verzenddatum.HasValue)
            {
                errors.Add(
                    new ValidationError("verzenddatum", ErrorCode.MustBeEmpty, $"Veld moet leeg zijn als aardrelatie '{AardRelatie.afzender}' is.")
                );
            }
        }
        else if (existingVerzending.AardRelatie == AardRelatie.geadresseerde)
        {
            if (!existingVerzending.Verzenddatum.HasValue)
            {
                errors.Add(
                    new ValidationError(
                        "verzenddatum",
                        ErrorCode.Required,
                        $"Veld moet gevuld zijn als aardrelatie '{AardRelatie.geadresseerde}' is."
                    )
                );
            }
            if (existingVerzending.Ontvangstdatum.HasValue)
            {
                errors.Add(
                    new ValidationError(
                        "ontvangstdatum",
                        ErrorCode.MustBeEmpty,
                        $"Veld moet leeg zijn als aardrelatie '{AardRelatie.geadresseerde}' is."
                    )
                );
            }
        }

        //
        // 3. Check relations

        if (
            informatieobject.Verzendingen.Any(v =>
                (!existingVerzendingId.HasValue || v.Id != existingVerzendingId) && v.AardRelatie == AardRelatie.afzender
            )
            && existingVerzending.AardRelatie == AardRelatie.afzender
        )
        {
            errors.Add(
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.Invalid,
                    "Een enkelvoudiginformatieobject kan slechts van één adres ontvangen (afzender) worden."
                )
            );
        }

        return errors.Count == 0;
    }
}
