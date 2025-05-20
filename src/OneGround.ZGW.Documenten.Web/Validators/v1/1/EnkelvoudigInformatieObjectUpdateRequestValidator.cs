using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._1.Requests;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._1;

public class EnkelvoudigInformatieObjectUpdateRequestValidator : ZGWValidator<EnkelvoudigInformatieObjectUpdateRequestDto>
{
    public EnkelvoudigInformatieObjectUpdateRequestValidator()
    {
        Include(new EnkelvoudigInformatieObjectRequestValidator());
    }
}
