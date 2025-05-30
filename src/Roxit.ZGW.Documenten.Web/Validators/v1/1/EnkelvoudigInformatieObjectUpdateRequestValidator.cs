using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1._1.Requests;

namespace Roxit.ZGW.Documenten.Web.Validators.v1._1;

public class EnkelvoudigInformatieObjectUpdateRequestValidator : ZGWValidator<EnkelvoudigInformatieObjectUpdateRequestDto>
{
    public EnkelvoudigInformatieObjectUpdateRequestValidator()
    {
        Include(new EnkelvoudigInformatieObjectRequestValidator());
    }
}
