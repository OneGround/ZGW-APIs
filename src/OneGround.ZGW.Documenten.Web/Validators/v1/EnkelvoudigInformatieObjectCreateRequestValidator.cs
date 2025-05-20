using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;

namespace OneGround.ZGW.Documenten.Web.Validators.v1;

public class EnkelvoudigInformatieObjectCreateRequestValidator : ZGWValidator<EnkelvoudigInformatieObjectCreateRequestDto>
{
    public EnkelvoudigInformatieObjectCreateRequestValidator()
    {
        Include(new EnkelvoudigInformatieObjectRequestValidator());
    }
}
