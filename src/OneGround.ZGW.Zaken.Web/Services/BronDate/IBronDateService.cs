using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Services.BronDate;

public interface IBronDateService
{
    Task<DateOnly?> GetAsync(Zaak zaak, List<ArchiveValidationError> errors, CancellationToken cancellationToken);
}
