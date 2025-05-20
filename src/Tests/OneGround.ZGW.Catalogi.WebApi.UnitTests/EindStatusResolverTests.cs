using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Services;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests;

public class EindStatusResolverTests
{
    private readonly UnitTestZtcDbContext _mockDbContext;
    private readonly EindStatusResolver _eindStatusResolver;

    public EindStatusResolverTests()
    {
        var logger = Mock.Of<ILogger<EindStatusResolver>>();
        _mockDbContext = GetMockedDbContext();
        _eindStatusResolver = new EindStatusResolver(logger, _mockDbContext);
    }

    [Fact]
    public async Task ResolveMultipleIsEindStatuses()
    {
        var cancellationToken = default(CancellationToken);
        var matchedZaakTypeId = Guid.NewGuid();
        var unmatchedZaakTypeId = Guid.NewGuid();

        var statusTypes = new[]
        {
            new StatusType
            {
                VolgNummer = 1,
                ZaakTypeId = matchedZaakTypeId,
                Omschrijving = "omschrijving",
                Owner = "000000000",
            },
            new StatusType
            {
                VolgNummer = 2,
                ZaakTypeId = matchedZaakTypeId,
                Omschrijving = "omschrijving",
                Owner = "000000000",
            },
            new StatusType
            {
                VolgNummer = 3,
                ZaakTypeId = unmatchedZaakTypeId,
                Omschrijving = "omschrijving",
                Owner = "000000000",
            },
        };
        _mockDbContext.StatusTypen.AddRange(statusTypes);
        await _mockDbContext.SaveChangesAsync();

        await _eindStatusResolver.ResolveAsync(statusTypes, cancellationToken);

        Assert.False(statusTypes[0].IsEindStatus);
        Assert.True(statusTypes[1].IsEindStatus);
        Assert.True(statusTypes[2].IsEindStatus);
    }

    [Fact]
    public async Task ResolveIsEindStatus_WhenMaximumVolgNummer_IsEindStatus()
    {
        var cancellationToken = default(CancellationToken);
        var matchedZaakTypeId = Guid.NewGuid();

        var statusTypes = new[]
        {
            new StatusType
            {
                VolgNummer = 1,
                ZaakTypeId = matchedZaakTypeId,
                Omschrijving = "omschrijving",
                Owner = "000000000",
            },
            new StatusType
            {
                VolgNummer = 2,
                ZaakTypeId = matchedZaakTypeId,
                Omschrijving = "omschrijving",
                Owner = "000000000",
            },
        };
        _mockDbContext.StatusTypen.AddRange(statusTypes);
        await _mockDbContext.SaveChangesAsync();

        await _eindStatusResolver.ResolveAsync(statusTypes[1], cancellationToken);

        Assert.True(statusTypes[1].IsEindStatus);
    }

    [Fact]
    public async Task ResolveIsEindStatus_WhenNotMaximumVolgNummer_IsNotEindStatus()
    {
        var cancellationToken = default(CancellationToken);
        var matchedZaakTypeId = Guid.NewGuid();

        var statusTypes = new[]
        {
            new StatusType
            {
                VolgNummer = 1,
                ZaakTypeId = matchedZaakTypeId,
                Omschrijving = "omschrijving",
                Owner = "000000000",
            },
            new StatusType
            {
                VolgNummer = 2,
                ZaakTypeId = matchedZaakTypeId,
                Omschrijving = "omschrijving",
                Owner = "000000000",
            },
        };
        _mockDbContext.StatusTypen.AddRange(statusTypes);
        await _mockDbContext.SaveChangesAsync();

        await _eindStatusResolver.ResolveAsync(statusTypes[0], cancellationToken);

        Assert.False(statusTypes[0].IsEindStatus);
    }

    private static UnitTestZtcDbContext GetMockedDbContext()
    {
        var options = new DbContextOptionsBuilder<ZtcDbContext>().UseInMemoryDatabase(databaseName: $"ztc").Options;

        var context = new UnitTestZtcDbContext(options);

        return context;
    }
}
