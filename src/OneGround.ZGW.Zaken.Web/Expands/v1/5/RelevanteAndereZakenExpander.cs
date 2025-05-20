using System;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class RelevanteAndereZakenExpander : ZakenCollectionExpander
{
    public RelevanteAndereZakenExpander(IExpanderFactory expanderFactory, IServiceProvider serviceProvider, IEntityUriService uriService)
        : base(expanderFactory, serviceProvider, uriService) { }

    public override string ExpandName => "relevanteanderezaken";
}
