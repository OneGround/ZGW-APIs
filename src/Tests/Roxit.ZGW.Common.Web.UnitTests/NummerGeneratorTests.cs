using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Roxit.ZGW.Common.Web.Services.NumberGenerator;
using Roxit.ZGW.DataAccess.NumberGenerator;
using Xunit;

namespace Roxit.ZGW.Common.Web.UnitTests;

public class NummerGeneratorTests
{
    private readonly IConfigurationRoot _mockedConfiguration;
    private readonly Mock<ILogger<NummerGenerator<UnitTestDbContext>>> _loggerMock;
    private readonly Mock<ISqlCommandExecutor> _sqlCommandExecutor;
    private readonly DateTime _now = DateTime.Now;
    private readonly string _rsin = "813264571";

    public NummerGeneratorTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { "Application:NummerGeneratorFormats:zaken", "Z{yyyy}-{v^8}" },
            { "Application:NummerGeneratorFormats:besluiten", "B{yyyy}-{v^8}" },
        };

        _mockedConfiguration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        _loggerMock = new Mock<ILogger<NummerGenerator<UnitTestDbContext>>>();
        _sqlCommandExecutor = new Mock<ISqlCommandExecutor>();
    }

    [Theory]
    [MemberData(nameof(FormatingData))]
    public async Task GenerateAsync_ShouldGenerateNewFormat(string entity, string expectedFormat)
    {
        var mockDbContext = new UnitTestDbContext(await GetMockedDbContext());
        var nummerGenerator = new NummerGenerator<UnitTestDbContext>(
            _mockedConfiguration,
            mockDbContext,
            _loggerMock.Object,
            _sqlCommandExecutor.Object
        );
        var result = await nummerGenerator.GenerateAsync(_rsin, entity);
        Assert.Equal(expectedFormat, result);
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateUniqueFormat()
    {
        var mockDbContext = new UnitTestDbContext(await GetMockedDbContext());
        var nummerGenerator = new NummerGenerator<UnitTestDbContext>(
            _mockedConfiguration,
            mockDbContext,
            _loggerMock.Object,
            _sqlCommandExecutor.Object
        );
        var result = await nummerGenerator.GenerateAsync(_rsin, "besluiten", format => format != $"B{_now.Year}-00000002");
        Assert.Equal($"B{_now.Year}-00000003", result);
    }

    private async Task<DbContextOptions<UnitTestDbContext>> GetMockedDbContext()
    {
        var options = new DbContextOptionsBuilder<UnitTestDbContext>().UseInMemoryDatabase(databaseName: $"db-{Guid.NewGuid()}").Options;

        using (var context = new UnitTestDbContext(options))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.OrganisatieNummers.Add(
                new OrganisatieNummer
                {
                    Id = 1,
                    Rsin = _rsin,
                    Formaat = "B{yyyy}-{v^8}",
                    EntiteitNaam = "besluiten",
                    HuidigNummer = 1,
                    HuidigNummerEntiteit = "B2024-00000001",
                }
            );

            await context.SaveChangesAsync();
        }

        return options;
    }

    public static IEnumerable<object[]> FormatingData()
    {
        var now = DateTime.UtcNow;
        return
        [
            //first format
            ["zaken", $"Z{now.Year}-00000001"],
            //when format already exist
            ["besluiten", $"B{now.Year}-00000002"],
        ];
    }
}

public class UnitTestDbContext : DbContext, IDbContextWithNummerGenerator
{
    public UnitTestDbContext(DbContextOptions<UnitTestDbContext> options)
        : base(options) { }

    public DbSet<OrganisatieNummer> OrganisatieNummers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }
}
