using System.Collections.Generic;
using OneGround.ZGW.Catalogi.Contracts.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Zaken.Web.Services.BronDate;

namespace OneGround.ZGW.Zaken.Web.Services;

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
