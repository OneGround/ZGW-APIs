using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1._5.Requests;

namespace Roxit.ZGW.Documenten.Web.Validators.v1._5;

public class EnkelvoudigInformatieObjectCreateRequestValidator : ZGWValidator<EnkelvoudigInformatieObjectCreateRequestDto>
{
    public EnkelvoudigInformatieObjectCreateRequestValidator()
    {
        Include(new EnkelvoudigInformatieObjectRequestValidator());
    }
}
