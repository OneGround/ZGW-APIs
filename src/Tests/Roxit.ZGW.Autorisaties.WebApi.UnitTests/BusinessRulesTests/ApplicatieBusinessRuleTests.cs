using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Roxit.ZGW.Autorisaties.Common.BusinessRules;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Catalogi.ServiceAgent.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Xunit;

namespace Roxit.ZGW.Autorisaties.WebApi.UnitTests.BusinessRulesTests;

public class ApplicatieBusinessRuleTests
{
    private static readonly string _applicatieLabel = "applicatie1";
    private static readonly Guid _applicatieId = Guid.NewGuid();
    private static readonly string _ownerRSIN = "000001375";

    [Theory]
    [MemberData(nameof(AddAtorisatieData))]
    public async Task Applicatie_ApplicatieBusinessRuleService_Validation(
        Guid id,
        string label,
        bool heeftAlleAutorisaties,
        List<ApplicatieClient> clientIds,
        string ownerRSIN,
        int howManyErrorsExpected
    )
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var configuration = new Mock<IConfiguration>();
        var mockDbContext = new UnitTestAcDbContext(await GetMockedAcDbContext());

        var svc = new ApplicatieBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, configuration.Object);

        var newApplicatie = new Applicatie
        {
            Id = id,
            Label = label,
            HeeftAlleAutorisaties = heeftAlleAutorisaties,
            ClientIds = clientIds,
            Owner = ownerRSIN,
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateApplicatieAsync(newApplicatie, errors);

        var isErrorExpected = howManyErrorsExpected > 0;
        Assert.Equal(isErrorExpected, !valid);
        Assert.Equal(howManyErrorsExpected, errors.Count);
    }

    private static async Task<DbContextOptions<AcDbContext>> GetMockedAcDbContext()
    {
        var options = new DbContextOptionsBuilder<AcDbContext>().UseInMemoryDatabase(databaseName: $"acd-{Guid.NewGuid()}").Options;

        // Insert seed data into the database using one instance of the context
        using (var context = new UnitTestAcDbContext(options))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.Applicaties.Add(
                new Applicatie
                {
                    Id = _applicatieId,
                    Label = _applicatieLabel,
                    Owner = _ownerRSIN,
                }
            );
            context.Applicaties.Add(
                new Applicatie
                {
                    Id = Guid.NewGuid(),
                    Label = "Applicatie2",
                    Owner = _ownerRSIN,
                }
            );

            await context.SaveChangesAsync();
        }

        return options;
    }

    public static IEnumerable<object[]> AddAtorisatieData =>
        [
            //Applicatie is valid
            [
                Guid.NewGuid(),
                "Applicatie3",
                true,
                new List<ApplicatieClient> { CreateClient("oneground-1"), CreateClient("oneground-2") },
                _ownerRSIN,
                0,
            ],
            [_applicatieId, "Applicatie3", true, new List<ApplicatieClient> { CreateClient("oneground-1") }, _ownerRSIN, 0],
            [_applicatieId, _applicatieLabel, true, new List<ApplicatieClient> { CreateClient("oneground-1") }, _ownerRSIN, 0],
            //Cannot have multiple clientId(s) when the flag heeft_alle_autorisaties is not set
            [
                Guid.NewGuid(),
                "Applicatie3",
                false,
                new List<ApplicatieClient> { CreateClient("oneground-1"), CreateClient("oneground-2") },
                _ownerRSIN,
                1,
            ],
            //Label should be unique and case-sensetive per organization
            [Guid.NewGuid(), "Applicatie1", false, new List<ApplicatieClient>(), _ownerRSIN, 1],
            [Guid.NewGuid(), "Applicatie1", false, new List<ApplicatieClient>(), "000001376", 0],
            //ClientId's should be unique
            [
                Guid.NewGuid(),
                "Applicatie3",
                true,
                new List<ApplicatieClient> { CreateClient("oneground-1"), CreateClient("oneground-1") },
                _ownerRSIN,
                1,
            ],
        ];

    public static ApplicatieClient CreateClient(string clientId)
    {
        return new ApplicatieClient { ClientId = clientId };
    }
}
