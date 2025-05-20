using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.BusinessRulesTests;

public class ClosedZaakModificationBusinessRuleTest
{
    [Fact]
    public void ClosedZaakModificationBusinessRule_validates_closed_zaak()
    {
        //Arrange

        var errors = new List<ValidationError>();

        var zakenAuthorizationContextAccessor = new Mock<IAuthorizationContextAccessor>();
        var authorization = new AuthorizedApplication { HasAllAuthorizations = false, Authorizations = [] };
        var noForcedUpdateAuthorization = new AuthorizationContext(authorization, []);
        zakenAuthorizationContextAccessor.Setup(z => z.AuthorizationContext).Returns(noForcedUpdateAuthorization);

        var zaak = new Zaak() { Zaaktype = "zaaktype", Einddatum = new DateOnly() }; // Closed zaak
        var service = new ClosedZaakModificationBusinessRule(zakenAuthorizationContextAccessor.Object);

        //Act

        var failsClosedZaakCheck = service.ValidateClosedZaakModificationRule(zaak, errors);

        //Assert

        Assert.NotEmpty(errors);
        Assert.False(failsClosedZaakCheck);
    }
}
