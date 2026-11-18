using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Notificaties.DataModel;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Writes and reads kanalen and abonnementen straight in the database, bypassing the API under test.
/// </summary>
internal static class NotificatiesSeed
{
    public const string CallbackUrl = "https://consumer.integrationtest.invalid/callback";

    // A kanaal name is at most 50 characters
    public static string NewKanaalNaam() => $"it-{Guid.NewGuid():N}";

    public static string NewAuth() => $"Token integration-test-{Guid.NewGuid():N}";

    public static async Task<Kanaal> KanaalAsync(NotificatiesWebApplicationFactory factory, params string[] filters)
    {
        var kanaal = new Kanaal
        {
            Id = Guid.NewGuid(),
            Naam = NewKanaalNaam(),
            DocumentatieLink = "https://docs.integrationtest.invalid/kanaal",
            Filters = filters,
            CreationTime = DateTime.UtcNow,
        };

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NrcDbContext>();
        context.Kanalen.Add(kanaal);
        await context.SaveChangesAsync();

        return kanaal;
    }

    public static async Task<Abonnement> AbonnementAsync(NotificatiesWebApplicationFactory factory, string owner, Kanaal kanaal)
    {
        var abonnement = new Abonnement
        {
            Id = Guid.NewGuid(),
            Owner = owner,
            CallbackUrl = CallbackUrl,
            Auth = NewAuth(),
            AbonnementKanalen =
            [
                new AbonnementKanaal
                {
                    Id = Guid.NewGuid(),
                    KanaalId = kanaal.Id,
                    Filters = [],
                },
            ],
        };

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NrcDbContext>();
        context.Abonnementen.Add(abonnement);
        await context.SaveChangesAsync();

        return abonnement;
    }

    public static async Task<Abonnement> FindAbonnementAsync(NotificatiesWebApplicationFactory factory, Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NrcDbContext>();

        return await context.Abonnementen.AsNoTracking().Include(a => a.AbonnementKanalen).SingleOrDefaultAsync(a => a.Id == id);
    }

    public static async Task<List<Abonnement>> FindAbonnementenByAuthAsync(NotificatiesWebApplicationFactory factory, string auth)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NrcDbContext>();

        return await context.Abonnementen.AsNoTracking().Where(a => a.Auth == auth).ToListAsync();
    }

    public static async Task<Kanaal> FindKanaalByNaamAsync(NotificatiesWebApplicationFactory factory, string naam)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NrcDbContext>();

        return await context.Kanalen.AsNoTracking().SingleOrDefaultAsync(k => k.Naam == naam);
    }
}
