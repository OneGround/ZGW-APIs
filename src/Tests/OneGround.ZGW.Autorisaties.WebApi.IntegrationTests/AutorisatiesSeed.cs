using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.DataModel;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// Writes and reads applicaties straight in the database, bypassing the API under test.
/// </summary>
internal static class AutorisatiesSeed
{
    public static string NewClientId() => $"integration-test-{Guid.NewGuid():N}";

    public static async Task<Applicatie> ApplicatieAsync(
        AutorisatiesWebApplicationFactory factory,
        string owner,
        string clientId,
        bool heeftAlleAutorisaties = false,
        Autorisatie autorisatie = null
    )
    {
        var applicatie = new Applicatie
        {
            Id = Guid.NewGuid(),
            Owner = owner,
            Label = $"integration-test-{Guid.NewGuid():N}",
            CreationTime = DateTime.UtcNow,
            HeeftAlleAutorisaties = heeftAlleAutorisaties,
            ClientIds =
            [
                new ApplicatieClient
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    CreationTime = DateTime.UtcNow,
                },
            ],
            Autorisaties = autorisatie == null ? [] : [autorisatie],
        };

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AcDbContext>();
        context.Applicaties.Add(applicatie);
        await context.SaveChangesAsync();

        return applicatie;
    }

    public static Autorisatie Autorisatie(string owner, Component component, params string[] scopes)
    {
        return new Autorisatie
        {
            Id = Guid.NewGuid(),
            Owner = owner,
            Component = component,
            Scopes = scopes,
        };
    }

    public static async Task<Applicatie> FindAsync(AutorisatiesWebApplicationFactory factory, Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AcDbContext>();

        return await context.Applicaties.AsNoTracking().Include(a => a.ClientIds).SingleOrDefaultAsync(a => a.Id == id);
    }

    public static void AssertNoConsumerLookup(AutorisatiesWebApplicationFactory factory, string clientId)
    {
        Assert.DoesNotContain(
            factory.OutboundHttp.Requests,
            r =>
                r.RequestUri!.AbsolutePath.EndsWith("/applicaties/consumer", StringComparison.OrdinalIgnoreCase)
                && r.RequestUri.Query.Contains($"clientId={clientId}", StringComparison.OrdinalIgnoreCase)
        );
    }
}
