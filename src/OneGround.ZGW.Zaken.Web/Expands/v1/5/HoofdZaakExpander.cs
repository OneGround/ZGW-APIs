using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class HoofdZaakExpander : ZaakBaseExpander, IObjectExpander<string>
{
    private readonly IExpanderFactory _expanderFactory;

    public HoofdZaakExpander(IExpanderFactory expanderFactory, IServiceProvider serviceProvider, IEntityUriService uriService)
        : base(serviceProvider, uriService)
    {
        _expanderFactory = expanderFactory;
    }

    public string ExpandName => "hoofdzaak";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string hoofdzaakUrl)
    {
        if (hoofdzaakUrl == null)
        {
            // Most zaken has no hoofdzaak defined (null here) so return empty object
            return new object();
        }

        var (hoofdzaak, error) = await GetZaakAsync(hoofdzaakUrl);
        if (hoofdzaak == null)
        {
            return error;
        }

        // Note: Strip off parent expand name "hoofdzaak" and remove non-hoofdzaak pathes, so we can use the generic ZaakExpander (to resolve hoofdzaak data)
        // For example if path is: ?expand=rollen, hoofdzaak.rollen.roltype, status.statustype
        //  will look and extract as follow:
        //    expandLookup:
        //      -rollen
        //      -hoofdzaak.rollen.roltype
        //      -status.statustype
        //    innerExpandLookup:
        //      -rollen.roltype
        var innerExpandLookup = GetInnerExpandLookup(ExpandName, expandLookup);

        var expander = _expanderFactory.Create<ZaakResponseDto>("zaak");

        var hoofdzaakExpandedDto = await expander.ResolveAsync(innerExpandLookup, hoofdzaak);

        return hoofdzaakExpandedDto;
    }
}
