using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class GetAllInformatieObjectTypenQueryHandler
    : CatalogiBaseHandler<GetAllInformatieObjectTypenQueryHandler>,
        IRequestHandler<GetAllInformatieObjectTypenQuery, QueryResult<PagedResult<InformatieObjectType>>>
{
    private readonly IInformatieObjectTypeDataService _informatieObjectTypeDataService;

    public GetAllInformatieObjectTypenQueryHandler(
        ILogger<GetAllInformatieObjectTypenQueryHandler> logger,
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

    public async Task<QueryResult<PagedResult<InformatieObjectType>>> Handle(
        GetAllInformatieObjectTypenQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all InformatieObjectTypen....");

        var result = await _informatieObjectTypeDataService.GetAllAsync(
            request.Pagination.Page,
            request.Pagination.Size,
            request.GetAllInformatieObjectTypenFilter,
            cancellationToken
        );

        return new QueryResult<PagedResult<InformatieObjectType>>(result, QueryStatus.OK);
    }
}

class GetAllInformatieObjectTypenQuery : IRequest<QueryResult<PagedResult<InformatieObjectType>>>
{
    public GetAllInformatieObjectTypenFilter GetAllInformatieObjectTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
