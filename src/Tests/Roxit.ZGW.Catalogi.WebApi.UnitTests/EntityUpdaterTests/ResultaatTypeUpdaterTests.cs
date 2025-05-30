using AutoFixture;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;
using Roxit.ZGW.Common.DataModel;
using Xunit;

namespace Roxit.ZGW.Catalogi.WebApi.UnitTests.EntityUpdaterTests;

public class ResultaatTypeFieldUpdaterTests : UpdaterTests
{
    private readonly ResultaatTypeUpdater _updater;

    public ResultaatTypeFieldUpdaterTests()
    {
        _updater = new ResultaatTypeUpdater();
    }

    [Fact]
    public void UpdatesBaseProperties()
    {
        var request = _fixture.Create<ResultaatType>();
        request.ArchiefNominatie = ArchiefNominatie.blijvend_bewaren;

        var source = _fixture.Create<ResultaatType>();
        source.ArchiefNominatie = ArchiefNominatie.vernietigen;

        // act
        _updater.Update(request, source);

        Assert.Equal(request.ArchiefActieTermijn, source.ArchiefActieTermijn);
        Assert.Equal(request.ArchiefNominatie, source.ArchiefNominatie);
        Assert.Equal(request.Omschrijving, source.Omschrijving);
        Assert.Equal(request.ResultaatTypeOmschrijving, source.ResultaatTypeOmschrijving);
        Assert.Equal(request.SelectieLijstKlasse, source.SelectieLijstKlasse);
        Assert.Equal(request.Toelichting, source.Toelichting);
    }

    [Fact]
    public void UpdatesBronDatumArchiefProcedure()
    {
        // set different values for request and source properties,
        // because Fixture can create them identical
        var request = _fixture.Create<ResultaatType>();
        request.ArchiefNominatie = ArchiefNominatie.blijvend_bewaren;
        request.BronDatumArchiefProcedure.Afleidingswijze = Afleidingswijze.afgehandeld;
        request.BronDatumArchiefProcedure.EindDatumBekend = true;
        request.BronDatumArchiefProcedure.ObjectType = ObjectType.besluit;

        var source = _fixture.Create<ResultaatType>();
        source.ArchiefNominatie = ArchiefNominatie.vernietigen;
        source.BronDatumArchiefProcedure.Afleidingswijze = Afleidingswijze.eigenschap;
        source.BronDatumArchiefProcedure.EindDatumBekend = false;
        source.BronDatumArchiefProcedure.ObjectType = ObjectType.huishouden;

        // act
        _updater.Update(request, source);

        Assert.NotNull(source.BronDatumArchiefProcedure);
        Assert.Equal(request.BronDatumArchiefProcedure.Afleidingswijze, source.BronDatumArchiefProcedure.Afleidingswijze);
        Assert.Equal(request.BronDatumArchiefProcedure.DatumKenmerk, source.BronDatumArchiefProcedure.DatumKenmerk);
        Assert.Equal(request.BronDatumArchiefProcedure.EindDatumBekend, source.BronDatumArchiefProcedure.EindDatumBekend);
        Assert.Equal(request.BronDatumArchiefProcedure.ObjectType, source.BronDatumArchiefProcedure.ObjectType);
        Assert.Equal(request.BronDatumArchiefProcedure.ProcesTermijn, source.BronDatumArchiefProcedure.ProcesTermijn);
        Assert.Equal(request.BronDatumArchiefProcedure.Registratie, source.BronDatumArchiefProcedure.Registratie);

        // not-updatable properties
        Assert.Null(source.BronDatumArchiefProcedure.ResultaatType);
    }

    [Fact]
    public void Removes_BronDatumArchiefProcedure()
    {
        var request = _fixture.Create<ResultaatType>();
        request.BronDatumArchiefProcedure = null;
        var source = _fixture.Create<ResultaatType>();

        // act
        _updater.Update(request, source);

        Assert.Null(source.BronDatumArchiefProcedure);
    }

    [Fact]
    public void Adds_BronDatumArchiefProcedure()
    {
        var request = _fixture.Create<ResultaatType>();
        var source = _fixture.Create<ResultaatType>();
        source.BronDatumArchiefProcedure = null;

        // act
        _updater.Update(request, source);

        Assert.NotNull(source.BronDatumArchiefProcedure);
        Assert.Equal(request.BronDatumArchiefProcedure.Afleidingswijze, source.BronDatumArchiefProcedure.Afleidingswijze);
        Assert.Equal(request.BronDatumArchiefProcedure.DatumKenmerk, source.BronDatumArchiefProcedure.DatumKenmerk);
        Assert.Equal(request.BronDatumArchiefProcedure.EindDatumBekend, source.BronDatumArchiefProcedure.EindDatumBekend);
        Assert.Equal(request.BronDatumArchiefProcedure.ObjectType, source.BronDatumArchiefProcedure.ObjectType);
        Assert.Equal(request.BronDatumArchiefProcedure.ProcesTermijn, source.BronDatumArchiefProcedure.ProcesTermijn);
        Assert.Equal(request.BronDatumArchiefProcedure.Registratie, source.BronDatumArchiefProcedure.Registratie);
        Assert.Null(source.BronDatumArchiefProcedure.ResultaatType);
    }

    [Fact]
    public void DoesNotUpdateCreationTimeProperties()
    {
        var request = _fixture.Create<ResultaatType>();
        var source = _fixture.Create<ResultaatType>();

        _updater.Update(request, source);

        Assert.NotEqual(request.CreatedBy, source.CreatedBy);
        Assert.NotEqual(request.CreationTime, source.CreationTime);
        Assert.NotEqual(request.Id, source.Id);
        Assert.NotEqual(request.Url, source.Url);
    }
}
