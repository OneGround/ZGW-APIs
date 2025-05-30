using AutoFixture;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;
using Xunit;

namespace Roxit.ZGW.Catalogi.WebApi.UnitTests.EntityUpdaterTests;

public class EigenschapUpdaterTests : UpdaterTests
{
    private readonly EigenschapUpdater _updater;

    public EigenschapUpdaterTests()
    {
        _updater = new EigenschapUpdater();
    }

    [Fact]
    public void UpdatesBaseProperties()
    {
        var request = _fixture.Create<Eigenschap>();
        var source = _fixture.Create<Eigenschap>();

        // act
        _updater.Update(request, source);

        Assert.Equal(request.Naam, source.Naam);
        Assert.Equal(request.Definitie, source.Definitie);
        Assert.Equal(request.Toelichting, source.Toelichting);
    }

    [Fact]
    public void UpdatesSpecificatie()
    {
        // set different values for request and source properties,
        // because Fixture can create them identical
        var request = _fixture.Create<Eigenschap>();
        request.Specificatie.Formaat = Formaat.datum;

        var source = _fixture.Create<Eigenschap>();
        source.Specificatie.Formaat = Formaat.getal;

        // act
        _updater.Update(request, source);

        Assert.NotNull(source.Specificatie);
        Assert.Equal(request.Specificatie.Groep, source.Specificatie.Groep);
        Assert.Equal(request.Specificatie.Formaat, source.Specificatie.Formaat);
        Assert.Equal(request.Specificatie.Kardinaliteit, source.Specificatie.Kardinaliteit);
        Assert.Equal(request.Specificatie.Lengte, source.Specificatie.Lengte);
        Assert.Equal(request.Specificatie.Waardenverzameling, source.Specificatie.Waardenverzameling);

        // not-updatable properties
        Assert.Null(source.Specificatie.Eigenschap);
    }

    [Fact]
    public void Removes_BronDatumArchiefProcedure()
    {
        var request = _fixture.Create<Eigenschap>();
        request.Specificatie = null;
        var source = _fixture.Create<Eigenschap>();

        // act
        _updater.Update(request, source);

        Assert.Null(source.Specificatie);
    }

    [Fact]
    public void Adds_Specificatie()
    {
        var request = _fixture.Create<Eigenschap>();
        var source = _fixture.Create<Eigenschap>();
        source.Specificatie = null;

        // act
        _updater.Update(request, source);

        Assert.NotNull(source.Specificatie);
        Assert.Equal(source.Specificatie.Formaat, request.Specificatie.Formaat);
        Assert.Equal(source.Specificatie.Groep, request.Specificatie.Groep);
        Assert.Equal(source.Specificatie.Kardinaliteit, request.Specificatie.Kardinaliteit);
        Assert.Equal(source.Specificatie.Lengte, request.Specificatie.Lengte);
        Assert.Equal(source.Specificatie.Waardenverzameling, request.Specificatie.Waardenverzameling);
        Assert.Null(source.Specificatie.Eigenschap);
    }

    [Fact]
    public void DoesNotUpdateCreationTimeProperties()
    {
        var request = _fixture.Create<Eigenschap>();
        var source = _fixture.Create<Eigenschap>();

        _updater.Update(request, source);

        Assert.NotEqual(request.CreatedBy, source.CreatedBy);
        Assert.NotEqual(request.CreationTime, source.CreationTime);
        Assert.NotEqual(request.Id, source.Id);
        Assert.NotEqual(request.Url, source.Url);
    }
}
