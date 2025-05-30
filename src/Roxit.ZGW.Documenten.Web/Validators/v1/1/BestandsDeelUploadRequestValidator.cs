using FluentValidation;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1._1.Requests;

namespace Roxit.ZGW.Documenten.Web.Validators.v1._1;

public class BestandsDeelUploadRequestValidator : ZGWValidator<BestandsDeelUploadRequestDto>
{
    public BestandsDeelUploadRequestValidator()
    {
        CascadeRuleFor(r => r.Inhoud).NotNull().NotEmpty().WithErrorCode(ErrorCode.Required);
    }
}
