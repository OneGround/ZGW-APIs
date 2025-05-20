using FluentValidation;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._1.Requests;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._1;

public class BestandsDeelUploadRequestValidator : ZGWValidator<BestandsDeelUploadRequestDto>
{
    public BestandsDeelUploadRequestValidator()
    {
        CascadeRuleFor(r => r.Inhoud).NotNull().NotEmpty().WithErrorCode(ErrorCode.Required);
    }
}
