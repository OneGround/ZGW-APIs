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
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class GetInformatieObjectTypeQueryHandler
    : CatalogiBaseHandler<GetInformatieObjectTypeQueryHandler>,
        IRequestHandler<GetInformatieObjectTypeQuery, QueryResult<InformatieObjectType>>
{
    private readonly IInformatieObjectTypeDataService _informatieObjectTypeDataService;

    public GetInformatieObjectTypeQueryHandler(
        ILogger<GetInformatieObjectTypeQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        INotificatieService notificatieService,
        IInformatieObjectTypeDataService informatieObjectTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _informatieObjectTypeDataService = informatieObjectTypeDataService;
    }

    public async Task<QueryResult<InformatieObjectType>> Handle(GetInformatieObjectTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get InformatieObjectType {Id}....", request.Id);

        var informatieObjectType = await _informatieObjectTypeDataService.GetAsync(request.Id, cancellationToken);
        if (informatieObjectType == null)
        {
            return new QueryResult<InformatieObjectType>(null, QueryStatus.NotFound);
        }

        return new QueryResult<InformatieObjectType>(informatieObjectType, QueryStatus.OK);
    }
}

class GetInformatieObjectTypeQuery : IRequest<QueryResult<InformatieObjectType>>
{
    public Guid Id { get; set; }
}
