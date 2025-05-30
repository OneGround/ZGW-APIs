using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Roxit.ZGW.Catalogi.ServiceAgent.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.ServiceAgent.v1;
using Roxit.ZGW.Notificaties.ServiceAgent;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.ServiceAgent.v1;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Xunit;

namespace Roxit.ZGW.Zaken.WebApi.UnitTests.BusinessRulesTests;

public class ZaakBusinessRuleTest
{
    [Fact]
    public async Task ZaakUpdate_With_Another_Identifier_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();
        var mockDocumentenServiceAgent = new Mock<IDocumentenServiceAgent>();
        var mockNotificatiesServiceAgent = new Mock<INotificatiesServiceAgent>();
        var mockEntitiUriService = new Mock<IEntityUriService>();

        var svc = new ZaakBusinessRuleService(
            null,
            mockEntitiUriService.Object,
            mockCatalogiServiceAgent.Object,
            mockZakenServiceAgent.Object,
            mockDocumentenServiceAgent.Object,
            mockNotificatiesServiceAgent.Object
        );

        var actualZaak = new Zaak { Identificatie = "12345", RelevanteAndereZaken = [] };
        var updateZaak = new Zaak { Identificatie = "55555", RelevanteAndereZaken = [] };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(actualZaak, updateZaak, "", errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
    }
}
