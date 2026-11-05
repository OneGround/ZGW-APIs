using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Configuration;

namespace OneGround.ZGW.Gesprekken.Web.Events;

public interface IAutomatischAntwoord
{
    /// <summary>
    /// Adds the automatic reply to a bijdrage of an inwoner, if configured: a gewone bijdrage of a system actor,
    /// marked automatisch. It is stored by the next SaveChanges.
    /// </summary>
    void Voegtoe(Gesprek gesprek, Gespreksdeelname verzender, DateTime moment);
}

public class AutomatischAntwoord : IAutomatischAntwoord
{
    private readonly GrcDbContext _context;
    private readonly IGesprekEventLog _eventLog;
    private readonly ApplicationConfiguration _configuration;

    public AutomatischAntwoord(GrcDbContext context, IGesprekEventLog eventLog, IConfiguration configuration)
    {
        _context = context;
        _eventLog = eventLog;
        _configuration = configuration.GetSection("Application").Get<ApplicationConfiguration>() ?? new ApplicationConfiguration();
    }

    public void Voegtoe(Gesprek gesprek, Gespreksdeelname verzender, DateTime moment)
    {
        if (verzender.Type != DeelnameType.partij || string.IsNullOrEmpty(_configuration.AutomatischAntwoordActor))
        {
            return;
        }

        var systeem = gesprek.Deelnames.FirstOrDefault(d => d.Actor == _configuration.AutomatischAntwoordActor);
        if (systeem == null)
        {
            systeem = new Gespreksdeelname
            {
                Owner = gesprek.Owner,
                Type = DeelnameType.actor,
                Actor = _configuration.AutomatischAntwoordActor,
                Naam = _configuration.AutomatischAntwoordNaam,
                AanvangsmomentDeelname = moment,
            };
            gesprek.Deelnames.Add(systeem);
            _eventLog.Append(gesprek, EventTypes.GespreksdeelnameAangemaakt, systeem);
        }

        // Just after the bijdrage it answers, so the order in the gesprek is stable
        var antwoord = new Gespreksbijdrage
        {
            Owner = gesprek.Owner,
            Gesprek = gesprek,
            Verzender = systeem,
            TekstGespreksBijdrage = _configuration.AutomatischAntwoordTekst,
            Automatisch = true,
            Leveringsmoment = moment.AddMilliseconds(1),
        };
        _context.Gespreksbijdragen.Add(antwoord);
        _eventLog.Append(gesprek, EventTypes.GespreksbijdrageAangemaakt, antwoord);
    }
}
