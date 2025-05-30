using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NodaTime.Text;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Services.BronDate;

public class TermijnBronDate : IBronDateService
{
    private readonly Period _procesTermijn;

    public TermijnBronDate(string procesTermijn)
    {
        if (procesTermijn != null)
        {
            _procesTermijn = PeriodPattern.NormalizingIso.Parse(procesTermijn).Value;
        }
    }

    public Task<DateOnly?> GetAsync(Zaak zaak, List<ArchiveValidationError> errors, CancellationToken cancellationToken)
    {
        if (!zaak.Einddatum.HasValue)
        {
            return Task.FromResult<DateOnly?>(null);
        }

        if (_procesTermijn == null)
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    "Geen procestermijn aanwezig voor het bepalen van de brondatum."
                )
            );
            return Task.FromResult<DateOnly?>(null);
        }

        var result = zaak.Einddatum.Value.AddYears(_procesTermijn.Years).AddMonths(_procesTermijn.Months).AddDays(_procesTermijn.Days);

        return Task.FromResult<DateOnly?>(result);
    }
}
