using System.Collections.Generic;
using Roxit.ZGW.Catalogi.Contracts.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Zaken.Web.Services.BronDate;

namespace Roxit.ZGW.Zaken.Web.Services;

public interface IBronDateServiceFactory
{
    IBronDateService Create(ResultaatTypeDto resultaatType, List<ArchiveValidationError> errors);
}

public class ArchiveValidationError : ValidationError
{
    public ArchiveValidationError(string name, string code, string reason, bool warning = true)
        : base(name, code, reason)
    {
        Warning = warning;
    }

    public bool Warning { get; private set; }
}
