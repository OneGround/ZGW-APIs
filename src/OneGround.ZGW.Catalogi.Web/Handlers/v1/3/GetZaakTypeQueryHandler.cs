using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class GetZaakTypeQueryHandler : CatalogiBaseHandler<GetZaakTypeQueryHandler>, IRequestHandler<GetZaakTypeQuery, QueryResult<ZaakType>>
{
    private readonly IZaakTypeDataService _zaakTypeDataService;

    public GetZaakTypeQueryHandler(
        ILogger<GetZaakTypeQueryHandler> logger,
        IEntityUriService uriService,
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakTypeDataService zaakTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _zaakTypeDataService = zaakTypeDataService;
    }

    public async Task<QueryResult<ZaakType>> Handle(GetZaakTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakType {Id}....", request.Id);

        var zaakType = await _zaakTypeDataService.GetAsync(request.Id, cancellationToken: cancellationToken);
        if (zaakType == null)
        {
            return new QueryResult<ZaakType>(null, QueryStatus.NotFound);
        }

        return new QueryResult<ZaakType>(zaakType, QueryStatus.OK);
    }
}

class GetZaakTypeQuery : IRequest<QueryResult<ZaakType>>
{
    public Guid Id { get; set; }
}
