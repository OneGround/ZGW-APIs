using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.IntegrationTests.Common;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// The number generator on the real PostgreSQL database, used by two requests (each with its own scope and database context) that take turns.
/// A request that takes several numbers (a batch reservation) must see the numbers the other request took in between.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class NummerGeneratorInterleavingTests
{
    private const string Entity = "zaken";

    private readonly ZakenWebApplicationFactory _factory;

    public NummerGeneratorInterleavingTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task Requests_that_take_turns_never_get_the_same_number()
    {
        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var generatorA = scopeA.ServiceProvider.GetRequiredService<INummerGenerator>();
        var generatorB = scopeB.ServiceProvider.GetRequiredService<INummerGenerator>();

        var numbers = new List<string>
        {
            await generatorA.GenerateAsync(TestRsins.D, Entity),
            await generatorB.GenerateAsync(TestRsins.D, Entity),
            await generatorA.GenerateAsync(TestRsins.D, Entity),
            await generatorB.GenerateAsync(TestRsins.D, Entity),
            await generatorA.GenerateAsync(TestRsins.D, Entity),
        };

        Assert.Equal(numbers.Count, numbers.Distinct().Count());
        Assert.Equal(numbers.Order(), numbers);
    }

    [Fact]
    public async Task A_batch_does_not_hand_out_the_numbers_of_a_concurrent_request()
    {
        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var generatorA = scopeA.ServiceProvider.GetRequiredService<INummerGenerator>();
        var generatorB = scopeB.ServiceProvider.GetRequiredService<INummerGenerator>();

        var batchA = new List<string>();
        var batchB = new List<string>();

        for (var i = 0; i < 10; i++)
        {
            batchA.Add(await generatorA.GenerateAsync(TestRsins.E, Entity, _ => true));
            batchB.Add(await generatorB.GenerateAsync(TestRsins.E, Entity, _ => true));
        }

        var all = batchA.Concat(batchB).ToList();

        Assert.Equal(all.Count, all.Distinct().Count());
    }
}
