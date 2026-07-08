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
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        // callingAssembly = the AC Web assembly (where the IRegisters live).
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        // Applicatie.Url is a computed, read-only property ($"/applicaties/{Id}"), so it can't be
        // set directly via an object initializer (unlike the plan's literal `Url = "/applicaties/x"`,
        // which does not compile against the real DataModel type). Setting Id and asserting against
        // the entity's own computed Url exercises the same resolver path: the mocked
        // IEntityUriService.GetUri returns e.Url, so the response DTO's Url should equal the
        // source entity's Url.
        var applicatie = new Applicatie
        {
            Id = Guid.NewGuid(),
            Label = "test",
            ClientIds = new List<ApplicatieClient>(),
        };

        var result = mapper.Map<ApplicatieResponseDto>(applicatie);

        Assert.Equal(applicatie.Url, result.Url);
    }
}
