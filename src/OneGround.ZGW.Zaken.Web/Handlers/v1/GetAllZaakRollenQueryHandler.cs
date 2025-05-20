using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Models.v1;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class GetAllZaakRollenQueryHandler
    : ZakenBaseHandler<GetAllZaakRollenQueryHandler>,
        IRequestHandler<GetAllZaakRolQuery, QueryResult<PagedResult<ZaakRol>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZaakRollenQueryHandler(
        ILogger<GetAllZaakRollenQueryHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakAuthorizationTempTableService zaakAuthorizationTempTableService
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
        _zaakAuthorizationTempTableService = zaakAuthorizationTempTableService;
    }

    public async Task<QueryResult<PagedResult<ZaakRol>>> Handle(GetAllZaakRolQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ZaakRollen....");

        var filter = GetZaakRolFilterPredicate(request.GetAllZaakRolFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakRol>();

        var query = _context.ZaakRollen.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(_context.TempZaakAuthorization, o => o.Zaak.Zaaktype, i => i.ZaakType, (r, a) => new { ZaakRol = r, Authorisatie = a })
                .Where(r => (int)r.ZaakRol.Zaak.VertrouwelijkheidAanduiding <= r.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(r => r.ZaakRol);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(z => z.Zaak)
            .Include(z => z.NatuurlijkPersoon.Verblijfsadres)
            .Include(z => z.NatuurlijkPersoon.SubVerblijfBuitenland)
            .Include(z => z.NietNatuurlijkPersoon.SubVerblijfBuitenland)
            .Include(z => z.Vestiging.Verblijfsadres)
            .Include(z => z.Vestiging.SubVerblijfBuitenland)
            .Include(z => z.OrganisatorischeEenheid)
            .Include(z => z.Medewerker)
            .OrderBy(z => z.CreationTime)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ZaakRol> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ZaakRol>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakRol, bool>> GetZaakRolFilterPredicate(GetAllZaakRollenFilter filter)
    {
        return z =>
            (filter.Zaak == null || z.Zaak.Id == _uriService.GetId(filter.Zaak))
            && (filter.Betrokkene == null || z.Betrokkene == filter.Betrokkene)
            && (!filter.BetrokkeneType.HasValue || z.BetrokkeneType == filter.BetrokkeneType.Value)
            && (filter.NatuurlijkPersoonInpBsn == null || z.NatuurlijkPersoon.InpBsn == filter.NatuurlijkPersoonInpBsn)
            && (filter.NatuurlijkPersoonAnpIdentificatie == null || z.NatuurlijkPersoon.AnpIdentificatie == filter.NatuurlijkPersoonAnpIdentificatie)
            && (filter.NatuurlijkPersoonInpANummer == null || z.NatuurlijkPersoon.InpANummer == filter.NatuurlijkPersoonInpANummer)
            && (filter.NietNatuurlijkPersoonInnNnpId == null || z.NietNatuurlijkPersoon.InnNnpId == filter.NietNatuurlijkPersoonInnNnpId)
            && (
                filter.NietNatuurlijkPersoonAnnIdentificatie == null
                || z.NietNatuurlijkPersoon.AnnIdentificatie == filter.NietNatuurlijkPersoonAnnIdentificatie
            )
            && (filter.VestigingNummer == null || z.Vestiging.VestigingsNummer == filter.VestigingNummer)
            && (
                filter.OrganisatorischeEenheidIdentificatie == null
                || z.OrganisatorischeEenheid.Identificatie == filter.OrganisatorischeEenheidIdentificatie
            )
            && (filter.MedewerkerIdentificatie == null || z.Medewerker.Identificatie == filter.MedewerkerIdentificatie)
            && (filter.RolType == null || z.RolType == filter.RolType)
            && (filter.Omschrijving == null || z.Omschrijving == filter.Omschrijving)
            && (!filter.OmschrijvingGeneriek.HasValue || z.OmschrijvingGeneriek == filter.OmschrijvingGeneriek.Value);
    }
}

class GetAllZaakRolQuery : IRequest<QueryResult<PagedResult<ZaakRol>>>
{
    public GetAllZaakRollenFilter GetAllZaakRolFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
