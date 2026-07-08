using System;
using System.Collections.Generic;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

public class AcMapsterWiringTests
{
    [Fact]
    public void AddZgwMapster_discovers_AC_registers_from_the_web_assembly()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        // callingAssembly = the AC Web assembly (where the IRegisters live).
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        // Applicatie.Url is a computed, read-only property ($"/applicaties/{Id}"), so it can't be
        // set directly via an object initializer (unlike the plan's literal `Url = "/applicaties/x"`,
        // which does not compile against the real DataModel type). Id is set only so the entity is
        // otherwise valid; its computed Url is intentionally never compared against below.
        var applicatie = new Applicatie
        {
            Id = Guid.NewGuid(),
            Label = "test",
            ClientIds = new List<ApplicatieClient>(),
        };

        var result = mapper.Map<ApplicatieResponseDto>(applicatie);

        // The mock returns a literal unrelated to Applicatie.Url's own computed value ($"/applicaties/{Id}"),
        // so this assertion only passes if MapsterUrlResolver actually called IEntityUriService.GetUri and
        // its return value flowed through — not if Mapster's default same-name-property convention copy
        // silently satisfied the assertion on its own (which it would if we compared against applicatie.Url
        // directly, since Applicatie.Url and ApplicatieResponseDto.Url share the same name/type).
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());

        // This is the one test that proves AC's REAL production path (through AddZgwMapster, with the
        // global EmptyCollectionIfNull transform) yields empty-not-null for a null source collection:
        // `applicatie.Autorisaties` is left null above, and convention-based nested mapping + the seam's
        // transform must produce an empty (non-null) list — matching AutoMapper's AllowNullCollections=false.
        Assert.NotNull(result.Autorisaties);
        Assert.Empty(result.Autorisaties);
    }
}
