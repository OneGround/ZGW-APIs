using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Models.v1;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

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
