using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Services.BronDate;

public class EigenschapBronDate : IBronDateService
{
    private readonly ZrcDbContext _context;
    private readonly string _datumKenmerk;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;

    public EigenschapBronDate(string datumKenmerk, ZrcDbContext context, ICatalogiServiceAgent catalogiServiceAgent)
    {
        _datumKenmerk = datumKenmerk;
        _context = context;
        _catalogiServiceAgent = catalogiServiceAgent;
    }

    public async Task<DateOnly?> GetAsync(Zaak zaak, List<ArchiveValidationError> errors, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_datumKenmerk))
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    "Geen datumkenmerk aanwezig om de eigenschap te achterhalen voor het bepalen van de brondatum."
                )
            );
            return null;
        }

        var resultaattype = await GetResultaatTypeAsync(zaak.Resultaat.ResultaatType, errors);
        if (resultaattype?.BronDatumArchiefProcedure == null)
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    $"Geen brondatumArchiefprocedure aanwezig voor de eigenschap die overeenkomt met het datumkenmerk \"{_datumKenmerk}\" om de eigenschap te achterhalen voor het bepalen van de brondatum."
                )
            );
            return null;
        }

        var zaakEigenschap = await _context
            .ZaakEigenschappen.AsNoTracking()
            .SingleOrDefaultAsync(z => z.ZaakId == zaak.Id && z.Naam == resultaattype.BronDatumArchiefProcedure.DatumKenmerk, cancellationToken);

        if (zaakEigenschap == null)
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    $"Geen eigenschap gevonden die overeenkomt met het datumkenmerk \"{_datumKenmerk}\" voor het bepalen van de brondatum."
                )
            );
            return null;
        }

        var eigenschap = await GetEigenschapAsync(zaakEigenschap.Eigenschap, errors);
        if (eigenschap == null)
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    $"Geen eigenschap te achterhalen die overeenkomt met het datumkenmerk \"{_datumKenmerk}\" voor het bepalen van de het type van eigenschap voor brondatum."
                )
            );
            return null;
        }

        if (eigenschap.Specificatie == null || eigenschap.Specificatie.Formaat != "datum")
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    $"De specificatie van de eigenschap die overeenkomt met het datumkenmerk \"{_datumKenmerk}\" ontbreekt of het formaat is niet van het type datum."
                )
            );
            return null;
        }

        if (string.IsNullOrEmpty(zaakEigenschap.Waarde))
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    $"De eigenschap-waarde van de eigenschap die overeenkomt met het \"{_datumKenmerk}\" is niet gevuld."
                )
            );
            return null;
        }

        if (!DateOnly.TryParseExact(zaakEigenschap.Waarde, "yyyyMMdd", out var waarde))
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    $"Geen geldige datumwaarde \"{zaakEigenschap.Waarde}\" in eigenschap die overeenkomt met het datumkenmerk {_datumKenmerk}."
                )
            );
            return null;
        }

        return waarde;
    }

    private async Task<ResultaatTypeResponseDto> GetResultaatTypeAsync(string resultaattypeUrl, List<ArchiveValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetResultaatTypeByUrlAsync(resultaattypeUrl);
        if (!result.Success)
        {
            errors.Add(new ArchiveValidationError("resultaattype", result.Error.Code, result.Error.Title));
            return null;
        }
        return result.Response;
    }

    private async Task<EigenschapResponseDto> GetEigenschapAsync(string eigenschapUrl, List<ArchiveValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetEigenschapByUrlAsync(eigenschapUrl);
        if (!result.Success)
        {
            errors.Add(new ArchiveValidationError("eigenschap", result.Error.Code, result.Error.Title));
            return null;
        }
        return result.Response;
    }
}
