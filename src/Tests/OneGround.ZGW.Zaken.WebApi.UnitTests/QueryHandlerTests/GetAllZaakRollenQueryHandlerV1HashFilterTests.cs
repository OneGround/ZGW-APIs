using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.Encryption;
using OneGround.ZGW.Zaken.Web.Handlers;
using OneGround.ZGW.Zaken.Web.Handlers.v1;
using OneGround.ZGW.Zaken.Web.Models.v1;
using OneGround.ZGW.Zaken.Web.Services;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.QueryHandlerTests;

public class GetAllZaakRollenQueryHandlerV1HashFilterTests
{
    private static IConfiguration BuildMinimalConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["Application:ZaakRollenPageSize"] = "100" })
            .Build();
    }

    private static ZrcDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ZrcDbContext>().UseInMemoryDatabase(databaseName: $"test-{System.Guid.NewGuid()}").Options;

        var databaseProtector = new Mock<IDatabaseProtector>();
        databaseProtector.Setup(p => p.Protect(It.IsAny<string>())).Returns<string>(s => s);
        databaseProtector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns<string>(s => s);

        var bsnHasher = new Mock<IBsnHasher>();
        bsnHasher.Setup(h => h.ComputeHash(It.IsAny<string>())).Returns<string>(s => s);

        return new ZrcDbContext(options, databaseProtector.Object, bsnHasher.Object);
    }

    [Fact]
    public async Task Handle_WithInpBsnFilter_CallsComputeHashWithCorrectValue()
    {
        // Arrange
        const string testBsn = "123456789";

        var bsnHasher = new Mock<IBsnHasher>();
        bsnHasher.Setup(h => h.ComputeHash(It.IsAny<string>())).Returns("abc123");

        using var context = CreateInMemoryContext();

        var authorization = new AuthorizedApplication { HasAllAuthorizations = true, Rsin = "000000000" };
        var authContext = new AuthorizationContext(authorization, []);
        var authContextAccessor = new Mock<IAuthorizationContextAccessor>();
        authContextAccessor.Setup(a => a.AuthorizationContext).Returns(authContext);

        var handler = new GetAllZaakRollenQueryHandler(
            NullLogger<GetAllZaakRollenQueryHandler>.Instance,
            BuildMinimalConfiguration(),
            context,
            new Mock<IEntityUriService>().Object,
            authContextAccessor.Object,
            new Mock<IZaakAuthorizationTempTableService>().Object,
            new Mock<IZaakKenmerkenResolver>().Object,
            bsnHasher.Object
        );

        var query = new GetAllZaakRolQuery
        {
            GetAllZaakRolFilter = new GetAllZaakRollenFilter { NatuurlijkPersoonInpBsn = testBsn },
            Pagination = new PaginationFilter { Page = 1, Size = 10 },
        };

        // Act
        // Note: The InMemory provider throws on model finalization due to the NodaTime Period type
        // in the ZrcDbContext. ComputeHash is still called synchronously before the first DB access,
        // so we can verify the mock call after catching the expected failure.
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(query, CancellationToken.None));

        // Assert – ComputeHash must have been called with the BSN before any DB operation
        bsnHasher.Verify(h => h.ComputeHash(testBsn), Times.Once);
    }
}
