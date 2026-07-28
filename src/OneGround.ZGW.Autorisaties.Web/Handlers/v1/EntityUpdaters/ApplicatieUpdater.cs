using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Common.Web;

namespace OneGround.ZGW.Autorisaties.Web.Handlers.v1.EntityUpdaters;

public class ApplicatieUpdater : IEntityUpdater<Applicatie>
{
    public void Update(Applicatie request, Applicatie source, decimal version = 1)
    {
        source.ClientIds = request.ClientIds;
        source.Label = request.Label;
        source.HeeftAlleAutorisaties = request.HeeftAlleAutorisaties;
        source.Autorisaties = request.Autorisaties;

        source.Autorisaties.ForEach(a => a.Owner = source.Owner);

        // Note: Field for v1.1 (check minimal version so it prevents older API versions from overwriting it with a default value)
        if (version >= 1.1M)
        {
            source.AlleenIsGereedVoorPublicatie = request.AlleenIsGereedVoorPublicatie;
        }
    }
}
