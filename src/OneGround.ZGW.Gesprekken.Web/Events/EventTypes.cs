namespace OneGround.ZGW.Gesprekken.Web.Events;

/// <summary>
/// CloudEvent types of the event stream, in the form nl.vng.&lt;api&gt;.&lt;resource&gt;.&lt;actie&gt;.
/// </summary>
public static class EventTypes
{
    private const string Prefix = "nl.vng.gesprekken.";

    public const string GesprekAangemaakt = Prefix + "gesprek.aangemaakt";
    public const string GesprekBeeindigd = Prefix + "gesprek.beeindigd";
    public const string GespreksbijdrageAangemaakt = Prefix + "gespreksbijdrage.aangemaakt";
    public const string GespreksbijdrageGelezen = Prefix + "gespreksbijdrage.gelezen";
    public const string GespreksbijdrageGeredigeerd = Prefix + "gespreksbijdrage.geredigeerd";
    public const string GespreksdeelnameAangemaakt = Prefix + "gespreksdeelname.aangemaakt";
    public const string GespreksdeelnameBeeindigd = Prefix + "gespreksdeelname.beeindigd";
}
