namespace OneGround.ZGW.Gesprekken.Web.Configuration;

public class ApplicationConfiguration
{
    public bool IgnoreZaakValidation { get; set; }
    public int EventStreamPollMilliseconds { get; set; } = 1000;
    public int EventStreamPingSeconds { get; set; } = 30;

    /// <summary>A gesprek closes after this many days without a new bijdrage; 0 turns closing by term off.</summary>
    public int AutomatischSluitenNaDagen { get; set; } = 3;
    public int AutomatischSluitenIntervalMinuten { get; set; } = 60;
    public long BijlageMaxBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>URL of the system actor that answers a bijdrage of an inwoner; empty turns the automatic reply off.</summary>
    public string AutomatischAntwoordActor { get; set; }
    public string AutomatischAntwoordNaam { get; set; } = "Automatisch bericht";
    public string AutomatischAntwoordTekst { get; set; } = "We reageren binnen 2 werkdagen.";
}
