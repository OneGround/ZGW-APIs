using System;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Zaken.Web.Expands.v1._5;

public class DeelZakenExpander : ZakenCollectionExpander
{
    public DeelZakenExpander(IExpanderFactory expanderFactory, IServiceProvider serviceProvider, IEntityUriService uriService)
        : base(expanderFactory, serviceProvider, uriService) { }

    public override string ExpandName => "deelzaken";
}
