using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Validators.v1;

public class ObjectInformatieObjectRequestValidator : ZGWValidator<ObjectInformatieObjectRequestDto>
{
    public ObjectInformatieObjectRequestValidator()
    {
        CascadeRuleFor(r => r.Object).NotNull().NotEmpty().IsUri().MaximumLength(200);
        CascadeRuleFor(r => r.InformatieObject).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(r => r.ObjectType).NotNull().NotEmpty().IsEnumName(typeof(ObjectType));
    }
}
