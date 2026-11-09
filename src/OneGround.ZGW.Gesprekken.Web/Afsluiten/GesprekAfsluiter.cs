using System;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Events;

namespace OneGround.ZGW.Gesprekken.Web.Afsluiten;

public interface IGesprekAfsluiter
{
    /// <summary>
    /// Closes a gesprek: a gesprek is a slow chat, so the contactmoment has only taken place now. The caller saves
    /// the changes. Closing by a deelnemer and closing after the term behave the same.
    /// </summary>
    Task SluitAsync(Gesprek gesprek, CancellationToken cancellationToken);
}

public class GesprekAfsluiter : IGesprekAfsluiter
{
    private readonly IKlantcontactService _klantcontacten;
    private readonly IGesprekEventLog _eventLog;

    public GesprekAfsluiter(IKlantcontactService klantcontacten, IGesprekEventLog eventLog)
    {
        _klantcontacten = klantcontacten;
        _eventLog = eventLog;
    }

    public async Task SluitAsync(Gesprek gesprek, CancellationToken cancellationToken)
    {
        gesprek.MomentEindeGesprek = DateTime.UtcNow;
        gesprek.Contactmoment = await _klantcontacten.MaakAanAsync(gesprek, cancellationToken);
        _eventLog.Append(gesprek, EventTypes.GesprekBeeindigd, gesprek);
    }
}
