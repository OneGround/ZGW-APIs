using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;

// Note: Mirrors v1._5.Queries.GetZaakQueryParametersValidator (the one targeting the old,
// unversioned type) for the new v1._7 concrete type. "expand" is deliberately NOT validated here --
// the v1._5 validator's ExpandsValid(SupportedExpands.GetAll("zaak")) check is against a hand-
// maintained, v1._5-only static list that predates (and is now bypassed by) the new expand engine.
// For v1.7, "expand" is validated by the new ExpandValidator<ZaakResponseDto> in the controller
// instead (see Controllers/v1/7/ZakenController.cs's GetAsync/HeadAsync).
public class GetZaakQueryParametersValidator : ZGWValidator<GetZaakQueryParameters>;
