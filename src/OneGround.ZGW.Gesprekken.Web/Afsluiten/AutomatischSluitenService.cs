using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Configuration;

namespace OneGround.ZGW.Gesprekken.Web.Afsluiten;

/// <summary>
/// Closes gesprekken without a new bijdrage for the configured term (default 3 days), so every gesprek eventually
/// leads to its contactmoment.
/// </summary>
public class AutomatischSluitenService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutomatischSluitenService> _logger;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public AutomatischSluitenService(
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<AutomatischSluitenService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>() ?? new ApplicationConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_applicationConfiguration.AutomatischSluitenNaDagen <= 0)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_applicationConfiguration.AutomatischSluitenIntervalMinuten));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SluitVerlopenGesprekkenAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(e, "Closing gesprekken after the term failed");
            }
        }
    }

    public async Task<int> SluitVerlopenGesprekkenAsync(DateTime nu, CancellationToken cancellationToken)
    {
        // URLs in events are built from the request; there is none here, so the API's own address stands in for it
        var api = new Uri(_configuration["Services:GRC:Api"] ?? "http://localhost");
        var context = new DefaultHttpContext();
        context.Request.Scheme = api.Scheme;
        context.Request.Host = new HostString(api.Authority);
        _httpContextAccessor.HttpContext = context;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrcDbContext>();
        var afsluiter = scope.ServiceProvider.GetRequiredService<IGesprekAfsluiter>();

        var grens = nu.AddDays(-_applicationConfiguration.AutomatischSluitenNaDagen);
        var verlopen = await db
            .Gesprekken.Include(g => g.Deelnames)
            .Where(g => g.MomentEindeGesprek == null)
            // Only a gesprek waiting for the inwoner: its last bijdrage (not an automatic one) is from the gemeente
            .Where(g =>
                g.Bijdragen.Where(b => !b.Automatisch)
                    .OrderByDescending(b => b.Leveringsmoment)
                    .Select(b => (DeelnameType?)b.Verzender.Type)
                    .FirstOrDefault() == DeelnameType.actor
            )
            .Where(g => g.Bijdragen.Where(b => !b.Automatisch).Max(b => (DateTime?)b.Leveringsmoment) < grens)
            .ToListAsync(cancellationToken);

        foreach (var gesprek in verlopen)
        {
            await afsluiter.SluitAsync(gesprek, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Closed {Count} gesprek(ken) after the term", verlopen.Count);
        return verlopen.Count;
    }
}
