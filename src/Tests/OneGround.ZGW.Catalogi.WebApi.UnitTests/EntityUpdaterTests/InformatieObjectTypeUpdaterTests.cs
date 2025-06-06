﻿using AutoFixture;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;
using OneGround.ZGW.Common.DataModel;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.EntityUpdaterTests;

public class InformatieObjectTypeUpdaterTests : UpdaterTests
{
    private readonly InformatieObjectTypeUpdater _updater;

    public InformatieObjectTypeUpdaterTests()
    {
        _updater = new InformatieObjectTypeUpdater();
    }

    [Fact]
    public void UpdatesBaseProperties()
    {
        var request = _fixture.Create<InformatieObjectType>();
        request.VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.confidentieel;

        var source = _fixture.Create<InformatieObjectType>();
        source.VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.beperkt_openbaar;

        // act
        _updater.Update(request, source);

        Assert.Equal(request.VertrouwelijkheidAanduiding, source.VertrouwelijkheidAanduiding);
        Assert.Equal(request.EindeGeldigheid, source.EindeGeldigheid);
        Assert.Equal(request.Omschrijving, source.Omschrijving);
        Assert.Equal(request.BeginGeldigheid, source.BeginGeldigheid);
    }
}
