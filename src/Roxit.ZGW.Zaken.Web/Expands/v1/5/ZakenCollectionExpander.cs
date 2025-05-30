using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.Contracts.v1._5.Responses;

namespace Roxit.ZGW.Zaken.Web.Expands.v1._5;

public abstract class ZakenCollectionExpander : ZaakBaseExpander, IObjectExpander<IEnumerable<string>>
{
    private readonly IExpanderFactory _expanderFactory;

    protected ZakenCollectionExpander(IExpanderFactory expanderFactory, IServiceProvider serviceProvider, IEntityUriService uriService)
        : base(serviceProvider, uriService)
    {
        _expanderFactory = expanderFactory;
    }

    public abstract string ExpandName { get; }

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, IEnumerable<string> innerzaakUrls)
    {
        var expander = _expanderFactory.Create<ZaakResponseDto>("zaak"); // Note:we reuse the standard zaak expander which handles all zaak related entities

        // Note: Get only the pathes from the current zaken collection
        // For example if ExpandName (defined in derived classes!) is "deelzaken" and path is: ?expand=rollen, deelzaken.rollen.roltype, status.statustype
        //  will look and extract as follow:
        //    expandLookup:
        //      -rollen
        //      -deelzaken.rollen.roltype
        //      -status.statustype
        //    innerExpandLookup:
        //      -rollen.roltype
        var innerExpandLookup = GetInnerExpandLookup(ExpandName, expandLookup);

        var innerZakenExpanded = new List<object>();

        foreach (var innerZaakUrl in innerzaakUrls)
        {
            var innerZaak = GetZaak(innerZaakUrl, out var error);
            if (innerZaak == null)
            {
                innerZakenExpanded.Add(error);
            }
            else
            {
                var innerZaakExpandedDto = await expander.ResolveAsync(innerExpandLookup, innerZaak);

                innerZakenExpanded.Add(innerZaakExpandedDto);
            }
        }
        return innerZakenExpanded;
    }
}
