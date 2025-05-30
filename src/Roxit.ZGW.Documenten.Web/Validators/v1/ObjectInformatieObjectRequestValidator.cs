using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1.Requests;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.Validators.v1;

public class ObjectInformatieObjectRequestValidator : ZGWValidator<ObjectInformatieObjectRequestDto>
{
    public ObjectInformatieObjectRequestValidator()
    {
        CascadeRuleFor(r => r.Object).NotNull().NotEmpty().IsUri().MaximumLength(200);
        CascadeRuleFor(r => r.InformatieObject).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(r => r.ObjectType).NotNull().NotEmpty().IsEnumName(typeof(ObjectType));
    }
}
