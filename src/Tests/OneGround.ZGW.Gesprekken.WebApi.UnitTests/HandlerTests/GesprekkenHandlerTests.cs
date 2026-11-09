using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Afsluiten;
using OneGround.ZGW.Gesprekken.Web.Bestanden;
using OneGround.ZGW.Gesprekken.Web.BusinessRules;
using OneGround.ZGW.Gesprekken.Web.Events;
using OneGround.ZGW.Gesprekken.Web.Handlers.v1;
using Xunit;

namespace OneGround.ZGW.Gesprekken.WebApi.UnitTests.HandlerTests;

public class GesprekkenHandlerTests : IDisposable
{
    private const string Tenant = "tenant-current";
    private static readonly Guid BestandId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private const string Zaak = "http://zaken.local/api/v1/zaken/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private const string Actor = "http://klantinteracties.local/api/v1/actoren/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    private const string Partij = "https://klantinteracties.example/api/v1/partijen/cccccccc-cccc-cccc-cccc-cccccccccccc";

    private readonly GrcDbContext _context;
    private readonly Mock<IGesprekBusinessRuleService> _businessRules = new();
    private readonly Mock<INummerGenerator> _nummerGenerator = new();
    private readonly Mock<IEntityUriService> _uriService = new();
    private readonly List<(string Type, object Resource)> _events = [];
    private readonly Mock<IGesprekEventLog> _eventLog = new();
    private readonly Mock<IKlantcontactService> _klantcontacten = new();
    private readonly Mock<IBestandService> _bestanden = new();
    private IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();

    public GesprekkenHandlerTests()
    {
        var options = new DbContextOptionsBuilder<GrcDbContext>().UseInMemoryDatabase($"grc-{Guid.NewGuid()}").Options;
        _context = new GrcDbContext(options);
        _context.Database.EnsureCreated();

        _businessRules.Setup(r => r.ValidateAanleidingZaakAsync(It.IsAny<string>(), It.IsAny<List<ValidationError>>())).ReturnsAsync(true);
        _bestanden
            .Setup(b => b.KoppelAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<Bijlage>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _nummerGenerator
            .Setup(g => g.GenerateAsync(It.IsAny<string>(), "gesprekken", It.IsAny<Func<string, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("G2026-00000001");
        _uriService.Setup(u => u.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => "http://gesprekken.local/api/v1" + e.Url);
        _eventLog
            .Setup(l => l.Append(It.IsAny<Gesprek>(), It.IsAny<string>(), It.IsAny<object>()))
            .Callback<Gesprek, string, object>((_, type, resource) => _events.Add((type, resource)));
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Creating_a_gesprek_stores_deelnames_and_first_bijdrage_and_logs_each()
    {
        var result = await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Equal("G2026-00000001", result.Result.Referentie);
        Assert.Equal(2, await _context.Gespreksdeelnames.CountAsync());
        Assert.Single(await _context.Gespreksbijdragen.ToListAsync());

        Assert.Equal(
            [
                EventTypes.GesprekAangemaakt,
                EventTypes.GespreksdeelnameAangemaakt,
                EventTypes.GespreksdeelnameAangemaakt,
                EventTypes.GespreksbijdrageAangemaakt,
            ],
            _events.Select(e => e.Type).ToArray()
        );
    }

    [Fact]
    public async Task Creating_a_gesprek_fails_when_the_zaak_is_rejected()
    {
        _businessRules
            .Setup(r => r.ValidateAanleidingZaakAsync(It.IsAny<string>(), It.IsAny<List<ValidationError>>()))
            .Callback<string, List<ValidationError>>((_, errors) => errors.Add(new ValidationError("aanleidingZaak", ErrorCodes.ZaakGesloten, "")))
            .ReturnsAsync(false);

        var result = await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        Assert.Equal(ErrorCodes.ZaakGesloten, result.Errors.Single().Code);
        Assert.Empty(_events);
        Assert.Empty(await _context.Gesprekken.ToListAsync());
    }

    [Fact]
    public async Task A_bijdrage_to_an_ended_gesprek_is_rejected()
    {
        var gesprek = (await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None)).Result;
        await EindigGesprekHandler().Handle(new EindigGesprekCommand { Id = gesprek.Id }, CancellationToken.None);

        var result = await CreateBijdrageHandler()
            .Handle(
                new CreateGespreksbijdrageCommand
                {
                    Bijdrage = new Gespreksbijdrage { GesprekId = gesprek.Id, TekstGespreksBijdrage = "Nog een vraag" },
                    VerzenderId = BurgerDeelname(gesprek).Id,
                },
                CancellationToken.None
            );

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        Assert.Equal(ErrorCodes.GesprekBeeindigd, result.Errors.Single().Code);
    }

    [Fact]
    public async Task Ending_a_gesprek_twice_is_rejected_and_logs_once()
    {
        var gesprek = (await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None)).Result;
        _events.Clear();

        var first = await EindigGesprekHandler().Handle(new EindigGesprekCommand { Id = gesprek.Id }, CancellationToken.None);
        var second = await EindigGesprekHandler().Handle(new EindigGesprekCommand { Id = gesprek.Id }, CancellationToken.None);

        Assert.Equal(CommandStatus.OK, first.Status);
        Assert.Equal(CommandStatus.ValidationError, second.Status);
        Assert.Equal(EventTypes.GesprekBeeindigd, Assert.Single(_events).Type);
    }

    [Fact]
    public async Task Ending_a_gesprek_creates_the_klantcontact_and_keeps_its_url()
    {
        var gesprek = (await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None)).Result;
        _klantcontacten
            .Setup(k => k.MaakAanAsync(It.IsAny<Gesprek>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://klantinteracties.example/api/v1/klantcontacten/1");

        await EindigGesprekHandler().Handle(new EindigGesprekCommand { Id = gesprek.Id }, CancellationToken.None);

        Assert.Equal("https://klantinteracties.example/api/v1/klantcontacten/1", (await _context.Gesprekken.SingleAsync()).Contactmoment);
    }

    [Fact]
    public async Task A_bijdrage_of_the_inwoner_gets_the_automatic_reply_of_the_system_actor_once_configured()
    {
        var gesprek = (await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None)).Result;
        Assert.Empty(await _context.Gespreksbijdragen.Where(b => b.Automatisch).ToListAsync());

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string> { { "Application:AutomatischAntwoordActor", "http://klantinteracties.local/api/v1/actoren/systeem" } }
            )
            .Build();

        await CreateBijdrageHandler()
            .Handle(
                new CreateGespreksbijdrageCommand
                {
                    Bijdrage = new Gespreksbijdrage { GesprekId = gesprek.Id, TekstGespreksBijdrage = "Mijn vraag" },
                    VerzenderId = BurgerDeelname(gesprek).Id,
                },
                CancellationToken.None
            );

        var antwoord = await _context.Gespreksbijdragen.Include(b => b.Verzender).SingleAsync(b => b.Automatisch);
        Assert.Equal("We reageren binnen 2 werkdagen.", antwoord.TekstGespreksBijdrage);
        Assert.Equal("http://klantinteracties.local/api/v1/actoren/systeem", antwoord.Verzender.Actor);

        var vraag = await _context.Gespreksbijdragen.SingleAsync(b => b.TekstGespreksBijdrage == "Mijn vraag");
        Assert.True(antwoord.Leveringsmoment > vraag.Leveringsmoment);

        // The system actor joins the gesprek only once
        await CreateBijdrageHandler()
            .Handle(
                new CreateGespreksbijdrageCommand
                {
                    Bijdrage = new Gespreksbijdrage { GesprekId = gesprek.Id, TekstGespreksBijdrage = "Nog een vraag" },
                    VerzenderId = BurgerDeelname(gesprek).Id,
                },
                CancellationToken.None
            );
        Assert.Equal(2, await _context.Gespreksbijdragen.CountAsync(b => b.Automatisch));
        Assert.Single(await _context.Gespreksdeelnames.Where(d => d.Actor.EndsWith("/systeem")).ToListAsync());
    }

    [Fact]
    public async Task An_ended_deelname_cannot_send_a_bijdrage()
    {
        var gesprek = (await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None)).Result;
        var burger = BurgerDeelname(gesprek);
        await EindigDeelnameHandler().Handle(new EindigGespreksdeelnameCommand { Id = burger.Id }, CancellationToken.None);

        var result = await CreateBijdrageHandler()
            .Handle(
                new CreateGespreksbijdrageCommand
                {
                    Bijdrage = new Gespreksbijdrage { GesprekId = gesprek.Id, TekstGespreksBijdrage = "Hallo?" },
                    VerzenderId = burger.Id,
                },
                CancellationToken.None
            );

        Assert.Equal(ErrorCodes.DeelnameBeeindigd, result.Errors.Single().Code);
    }

    [Fact]
    public async Task A_bijdrage_from_a_new_deelname_logs_deelname_and_bijdrage()
    {
        var gesprek = (await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None)).Result;
        _events.Clear();

        var result = await CreateBijdrageHandler()
            .Handle(
                new CreateGespreksbijdrageCommand
                {
                    Bijdrage = new Gespreksbijdrage
                    {
                        GesprekId = gesprek.Id,
                        TekstGespreksBijdrage = "Zie bijlage",
                        Bijlagen = [new Bijlage { BestandId = BestandId }],
                    },
                    NieuweVerzender = new Gespreksdeelname
                    {
                        Type = DeelnameType.actor,
                        Actor = "http://klantinteracties.local/api/v1/actoren/collega",
                        Naam = "Afdeling Inkomen",
                    },
                },
                CancellationToken.None
            );

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Equal(
            BestandId,
            (await _context.Gespreksbijdragen.Include(b => b.Bijlagen).SingleAsync(b => b.Id == result.Result.Id)).Bijlagen[0].BestandId
        );
        Assert.Equal([EventTypes.GespreksdeelnameAangemaakt, EventTypes.GespreksbijdrageAangemaakt], _events.Select(e => e.Type).ToArray());
    }

    [Fact]
    public async Task Marking_as_read_twice_returns_the_same_registration_and_logs_once()
    {
        var gesprek = (await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None)).Result;
        var bijdrage = gesprek.Bijdragen.Single();
        var burger = BurgerDeelname(gesprek);
        _events.Clear();

        var command = new MarkeerGelezenCommand { GespreksbijdrageId = bijdrage.Id, GespreksdeelnameId = burger.Id };
        var first = await GelezenHandler().Handle(command, CancellationToken.None);
        var second = await GelezenHandler().Handle(command, CancellationToken.None);

        Assert.Equal(first.Result.Id, second.Result.Id);
        Assert.Equal(EventTypes.GespreksbijdrageGelezen, Assert.Single(_events).Type);
    }

    [Fact]
    public async Task Listing_filters_on_aanleidingZaak_and_partij()
    {
        await CreateGesprekHandler().Handle(NewGesprekCommand(), CancellationToken.None);
        var other = NewGesprekCommand();
        other.Gesprek.AanleidingZaak = "http://zaken.local/api/v1/zaken/other";
        await CreateGesprekHandler().Handle(other, CancellationToken.None);

        var handler = new GetAllGesprekkenQueryHandler(
            NullLogger<GetAllGesprekkenQueryHandler>.Instance,
            _configuration,
            _uriService.Object,
            _context,
            Client()
        );

        var byZaak = await handler.Handle(
            new GetAllGesprekkenQuery
            {
                Filter = new Web.Models.v1.GetAllGesprekkenFilter { AanleidingZaak = Zaak },
                PageSize = 10,
            },
            CancellationToken.None
        );
        var byPartij = await handler.Handle(
            new GetAllGesprekkenQuery
            {
                Filter = new Web.Models.v1.GetAllGesprekkenFilter { GespreksdeelnamePartij = Partij },
                PageSize = 10,
            },
            CancellationToken.None
        );

        Assert.Equal(1, byZaak.Result.Count);
        Assert.Equal(2, byPartij.Result.Count);
    }

    private static Gespreksdeelname BurgerDeelname(Gesprek gesprek) => gesprek.Deelnames.Single(d => d.Type == DeelnameType.partij);

    private static CreateGesprekCommand NewGesprekCommand() =>
        new()
        {
            Gesprek = new Gesprek { Gespreksonderwerp = "Aanvraag bijzondere bijstand", AanleidingZaak = Zaak },
            Ontvangers =
            [
                new Gespreksdeelname
                {
                    Type = DeelnameType.partij,
                    Partij = Partij,
                    Naam = "Piet",
                },
            ],
            Verzender = new Gespreksdeelname
            {
                Type = DeelnameType.actor,
                Actor = Actor,
                Naam = "Gemeente",
            },
            EersteBijdrage = new Gespreksbijdrage { TekstGespreksBijdrage = "Graag uitleg over uw aanvraag." },
        };

    private static IAuthorizationContextAccessor Client()
    {
        var accessor = new Mock<IAuthorizationContextAccessor>();
        accessor
            .Setup(a => a.AuthorizationContext)
            .Returns(
                new AuthorizationContext(
                    new AuthorizedApplication { Rsin = Tenant, HasAllAuthorizations = true },
                    [AuthorizationScopes.Gesprekken.Create]
                )
            );
        return accessor.Object;
    }

    private CreateGesprekCommandHandler CreateGesprekHandler() =>
        new(
            NullLogger<CreateGesprekCommandHandler>.Instance,
            _configuration,
            _context,
            _uriService.Object,
            _nummerGenerator.Object,
            _businessRules.Object,
            Client(),
            _eventLog.Object,
            _bestanden.Object,
            new AutomatischAntwoord(_context, _eventLog.Object, _configuration)
        );

    private EindigGesprekCommandHandler EindigGesprekHandler() =>
        new(
            NullLogger<EindigGesprekCommandHandler>.Instance,
            _configuration,
            _context,
            _uriService.Object,
            Client(),
            new GesprekAfsluiter(_klantcontacten.Object, _eventLog.Object)
        );

    private EindigGespreksdeelnameCommandHandler EindigDeelnameHandler() =>
        new(NullLogger<EindigGespreksdeelnameCommandHandler>.Instance, _configuration, _context, _uriService.Object, Client(), _eventLog.Object);

    private CreateGespreksbijdrageCommandHandler CreateBijdrageHandler() =>
        new(
            NullLogger<CreateGespreksbijdrageCommandHandler>.Instance,
            _configuration,
            _context,
            _uriService.Object,
            Client(),
            _eventLog.Object,
            _bestanden.Object,
            new AutomatischAntwoord(_context, _eventLog.Object, _configuration)
        );

    private MarkeerGelezenCommandHandler GelezenHandler() =>
        new(NullLogger<MarkeerGelezenCommandHandler>.Instance, _configuration, _context, _uriService.Object, Client(), _eventLog.Object);
}
