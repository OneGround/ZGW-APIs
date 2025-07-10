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

class GetBesluitTypeQueryHandler : CatalogiBaseHandler<GetBesluitTypeQueryHandler>, IRequestHandler<GetBesluitTypeQuery, QueryResult<BesluitType>>
{
    private readonly IBesluitTypeDataService _besluitTypeDataService;

    public GetBesluitTypeQueryHandler(
        ILogger<GetBesluitTypeQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitTypeDataService besluitTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _besluitTypeDataService = besluitTypeDataService;
    }

    public async Task<QueryResult<BesluitType>> Handle(GetBesluitTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get BesluitType {Id}....", request.Id);

        var besluitType = await _besluitTypeDataService.GetAsync(
            request.Id,
            includeSoftRelations: request.IncludeSoftRelations,
            cancellationToken: cancellationToken
        );
        if (besluitType == null)
        {
            return new QueryResult<BesluitType>(null, QueryStatus.NotFound);
        }

        return new QueryResult<BesluitType>(besluitType, QueryStatus.OK);
    }
}

class GetBesluitTypeQuery : IRequest<QueryResult<BesluitType>>
{
    public Guid Id { get; set; }
    public bool IncludeSoftRelations { get; set; } = true;
}
