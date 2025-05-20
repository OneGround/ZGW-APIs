using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Services.BronDate;

public class AfgehandeldBronDate : IBronDateService
{
    public Task<DateOnly?> GetAsync(Zaak zaak, List<ArchiveValidationError> _, CancellationToken cancellationToken)
    {
        return Task.FromResult(zaak.Einddatum);
    }
}
