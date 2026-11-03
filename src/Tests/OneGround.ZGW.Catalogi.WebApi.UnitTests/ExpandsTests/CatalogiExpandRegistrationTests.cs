using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.Web.Expands.v1._4;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The expand paths that each v1.4 response offers. A resolver that is not registered would only show as an "invalid expand" at request time.
/// </summary>
public class CatalogiExpandRegistrationTests
{
    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IEntityUriService>());
        services.AddCatalogiAPIExpands();
        return services.BuildServiceProvider();
    }

    private static string[] PathsOf<TResponse>(ServiceProvider provider) =>
        provider.GetServices<IExpandResolver<TResponse>>().Select(r => r.Path).Order().ToArray();

    [Fact]
    public void Catalogus_OffersTheTypesItRefersTo()
    {
        using var provider = BuildServices();

        Assert.Equal(["besluittypen", "informatieobjecttypen", "zaaktypen"], PathsOf<CatalogusResponseDto>(provider));
    }

    [Fact]
    public void ZaakType_OffersTheCatalogusAndTheTypesItRefersTo()
    {
        using var provider = BuildServices();

        Assert.Equal(
            [
                "besluittypen",
                "catalogus",
                "deelzaaktypen",
                "eigenschappen",
                "gerelateerdeZaaktypen",
                "informatieobjecttypen",
                "resultaattypen",
                "roltypen",
                "statustypen",
                "zaakobjecttypen",
            ],
            PathsOf<ZaakTypeResponseDto>(provider)
        );
    }

    [Fact]
    public void BesluitType_OffersTheCatalogusAndTheTypesItRefersTo()
    {
        using var provider = BuildServices();

        Assert.Equal(["catalogus", "informatieobjecttypen", "resultaattypen", "zaaktypen"], PathsOf<BesluitTypeResponseDto>(provider));
    }

    [Fact]
    public void InformatieObjectType_OffersItsCatalogus()
    {
        using var provider = BuildServices();

        Assert.Equal(["catalogus"], PathsOf<InformatieObjectTypeResponseDto>(provider));
    }
}
