using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Common.Web;

namespace Roxit.ZGW.Autorisaties.Web.Handlers.EntityUpdaters;

public class ApplicatieUpdater : IEntityUpdater<Applicatie>
{
    public void Update(Applicatie request, Applicatie source, decimal version = 1)
    {
        source.ClientIds = request.ClientIds;
        source.Label = request.Label;
        source.HeeftAlleAutorisaties = request.HeeftAlleAutorisaties;
        source.Autorisaties = request.Autorisaties;

        source.Autorisaties.ForEach(a => a.Owner = source.Owner);
    }
}
