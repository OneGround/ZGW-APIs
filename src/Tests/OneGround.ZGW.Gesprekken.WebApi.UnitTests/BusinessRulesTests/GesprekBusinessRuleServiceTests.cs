using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Gesprekken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.ServiceAgent.v1;
using Xunit;

namespace OneGround.ZGW.Gesprekken.WebApi.UnitTests.BusinessRulesTests;

public class GesprekBusinessRuleServiceTests
{
    private const string Zaak = "http://zaken.local/api/v1/zaken/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    [Fact]
    public async Task An_open_zaak_is_accepted()
    {
        var (service, errors) = Service(new ZaakResponseDto { Einddatum = null });

        Assert.True(await service.ValidateAanleidingZaakAsync(Zaak, errors));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task A_closed_zaak_is_rejected()
    {
        var (service, errors) = Service(new ZaakResponseDto { Einddatum = "2026-06-28" });

        Assert.False(await service.ValidateAanleidingZaakAsync(Zaak, errors));
        Assert.Equal(ErrorCodes.ZaakGesloten, Assert.Single(errors).Code);
    }

    private static (GesprekBusinessRuleService, List<ValidationError>) Service(ZaakResponseDto zaak)
    {
        var zaken = new Mock<IZakenServiceAgent>();
        zaken.Setup(z => z.GetZaakByUrlAsync(Zaak)).ReturnsAsync(new ServiceAgentResponse<ZaakResponseDto>(zaak));

        return (new GesprekBusinessRuleService(zaken.Object, new ConfigurationBuilder().Build()), []);
    }
}
