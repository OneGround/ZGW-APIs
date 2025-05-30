using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Roxit.ZGW.Catalogi.Contracts.v1;
using Roxit.ZGW.Catalogi.Contracts.v1.Requests;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Helpers;
using Roxit.ZGW.Common.Web.Mapping.ValueResolvers;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.MappingProfiles.v1;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<ZaakType, ZaakTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.VersieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VersieDatum)))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus))
            .ForMember(dest => dest.StatusTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<StatusType>>(src => src.StatusTypen))
            .ForMember(dest => dest.RolTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<RolType>>(src => src.RolTypen))
            .ForMember(dest => dest.ResultaatTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<ResultaatType>>(src => src.ResultaatTypen))
            .ForMember(dest => dest.Eigenschappen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<Eigenschap>>(src => src.Eigenschappen))
            .ForMember(dest => dest.VerlengingsTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.VerlengingsTermijn)))
            .ForMember(dest => dest.Servicenorm, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.Servicenorm)))
            .ForMember(dest => dest.Doorlooptijd, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.Doorlooptijd)))
            .ForMember(dest => dest.GerelateerdeZaakTypen, opt => opt.Ignore())
            .ForMember(
                dest => dest.InformatieObjectTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakTypeInformatieObjectTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<InformatieObjectType>>(src =>
                        src.ZaakTypeInformatieObjectTypen.Where(i => i.InformatieObjectType != null).Select(s => s.InformatieObjectType)
                    );
                }
            )
            .ForMember(
                dest => dest.DeelZaakTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakTypeDeelZaakTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakType>>(src =>
                        src.ZaakTypeDeelZaakTypen.Where(z => z.DeelZaakType != null).Select(s => s.DeelZaakType)
                    );
                }
            )
            .ForMember(
                dest => dest.BesluitTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakTypeBesluitTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<BesluitType>>(src =>
                        src.ZaakTypeBesluitTypen.Where(b => b.BesluitType != null).Select(b => b.BesluitType)
                    );
                }
            )
            .AfterMap<MapGerelateerdeZaakTypenResponse>();

        CreateMap<ReferentieProces, ReferentieProcesDto>();

        // Note: This map is used to merge an existing ZAAKTYPE with the PATCH operation
        CreateMap<ZaakType, ZaakTypeRequestDto>()
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.VersieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VersieDatum)))
            .ForMember(dest => dest.GerelateerdeZaakTypen, opt => opt.Ignore())
            .ForMember(
                dest => dest.DeelZaakTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakTypeDeelZaakTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakType>>(src =>
                        src.ZaakTypeDeelZaakTypen.Where(z => z.DeelZaakType != null).Select(s => s.DeelZaakType)
                    );
                }
            )
            .ForMember(
                dest => dest.BesluitTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakTypeBesluitTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<BesluitType>>(src =>
                        src.ZaakTypeBesluitTypen.Where(b => b.BesluitType != null).Select(b => b.BesluitType)
                    );
                }
            )
            .AfterMap<MapMergedGerelateerdeZaakTypenUrlBased>();

        CreateMap<StatusType, StatusTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.OmschrijvingGeneriek, opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.OmschrijvingGeneriek)))
            .ForMember(dest => dest.StatusTekst, opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.StatusTekst)));

        // Note: This map is used to merge an existing STATUSTYPE with the PATCH operation
        CreateMap<StatusType, StatusTypeRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType));

        CreateMap<RolType, RolTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(s => s.ZaakType));

        // Note: This map is used to merge an existing RolType with the PATCH operation
        CreateMap<RolType, RolTypeRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType));

        CreateMap<ZaakTypeInformatieObjectType, ZaakTypeInformatieObjectTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(s => s.ZaakType))
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.InformatieObjectTypeOmschrijving))
            .ForMember(dest => dest.StatusType, opt => opt.MapFrom<MemberUrlResolver, StatusType>(s => s.StatusType))
            .ForMember(
                dest => dest.InformatieObjectType,
                opt =>
                {
                    opt.MapFrom<MemberUrlResolver, InformatieObjectType>(src => src.InformatieObjectType);
                }
            );

        // Note: This map is used to merge an existing ZaakTypeInformatieObjectTypen with the PATCH operation
        CreateMap<ZaakTypeInformatieObjectType, ZaakTypeInformatieObjectTypeRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.StatusType, opt => opt.MapFrom<MemberUrlResolver, StatusType>(src => src.StatusType))
            .ForMember(
                dest => dest.InformatieObjectType,
                opt => opt.MapFrom<MemberUrlResolver, InformatieObjectType>(src => src.InformatieObjectType)
            );

        CreateMap<ResultaatType, ResultaatTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(s => s.ZaakType))
            .ForMember(dest => dest.ArchiefActieTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.ArchiefActieTermijn)));

        // Note: for PATCH operation
        CreateMap<ResultaatType, ResultaatTypeRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType));

        CreateMap<BronDatumArchiefProcedure, BronDatumArchiefProcedureDto>()
            .ForMember(dest => dest.ProcesTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.ProcesTermijn)));

        CreateMap<Catalogus, CatalogusResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakType>>(src => src.ZaakTypes))
            .ForMember(dest => dest.BesluitTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<BesluitType>>(src => src.BesluitTypes))
            .ForMember(
                dest => dest.InformatieObjectTypen,
                opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<InformatieObjectType>>(src => src.InformatieObjectTypes)
            );

        CreateMap<InformatieObjectType, InformatieObjectTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus))
            .ForMember(
                dest => dest.ZaakTypen,
                opt =>
                {
                    opt.PreCondition(src => src.InformatieObjectTypeZaakTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakType>>(src =>
                        src.InformatieObjectTypeZaakTypen.Where(z => z.ZaakType != null).Select(b => b.ZaakType)
                    );
                }
            )
            .ForMember(
                dest => dest.BesluitTypen,
                opt =>
                {
                    opt.PreCondition(src => src.InformatieObjectTypeBesluitTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<BesluitType>>(src =>
                        src.InformatieObjectTypeBesluitTypen.Where(z => z.BesluitType != null).Select(b => b.BesluitType)
                    );
                }
            );

        // Note: for PATCH operation
        CreateMap<InformatieObjectType, InformatieObjectTypeRequestDto>()
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus))
            .ForMember(dest => dest.ZaakTypen, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitTypen, opt => opt.Ignore());

        CreateMap<EigenschapSpecificatie, EigenschapSpecificatieDto>();
        CreateMap<Eigenschap, EigenschapResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType));

        // Note: for PATCH operation
        CreateMap<Eigenschap, EigenschapRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType));

        CreateMap<BesluitType, BesluitTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(
                dest => dest.ZaakTypen,
                opt =>
                {
                    opt.PreCondition(src => src.BesluitTypeZaakTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakType>>(src =>
                        src.BesluitTypeZaakTypen.Where(b => b.ZaakType != null).Select(b => b.ZaakType)
                    );
                }
            )
            .ForMember(
                dest => dest.InformatieObjectTypen,
                opt =>
                {
                    opt.PreCondition(src => src.BesluitTypeInformatieObjectTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<InformatieObjectType>>(src =>
                        src.BesluitTypeInformatieObjectTypen.Where(b => b.InformatieObjectType != null).Select(b => b.InformatieObjectType)
                    );
                }
            )
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus))
            .ForMember(dest => dest.ReactieTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.ReactieTermijn)))
            .ForMember(dest => dest.PublicatieTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.PublicatieTermijn)));

        // Note: for PATCH operation
        CreateMap<BesluitType, BesluitTypeRequestDto>()
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(
                dest => dest.ZaakTypen,
                opt =>
                {
                    opt.PreCondition(src => src.BesluitTypeZaakTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakType>>(src =>
                        src.BesluitTypeZaakTypen.Where(b => b.ZaakType != null).Select(b => b.ZaakType)
                    );
                }
            )
            .ForMember(
                dest => dest.InformatieObjectTypen,
                opt =>
                {
                    opt.PreCondition(src => src.BesluitTypeInformatieObjectTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<InformatieObjectType>>(src =>
                        src.BesluitTypeInformatieObjectTypen.Where(b => b.InformatieObjectType != null).Select(b => b.InformatieObjectType)
                    );
                }
            )
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus));
    }
}

public class MapGerelateerdeZaakTypenResponse : IMappingAction<ZaakType, ZaakTypeResponseDto>
{
    private readonly IEntityUriService _uriService;

    public MapGerelateerdeZaakTypenResponse(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public void Process(ZaakType src, ZaakTypeResponseDto dest, ResolutionContext context)
    {
        var gerelateerdeZaakTypen = new List<GerelateerdeZaaktypeDto>();

        foreach (var gerelateerdeZaakType in src.ZaakTypeGerelateerdeZaakTypen.Where(z => z.GerelateerdeZaakType != null))
        {
            var item = new GerelateerdeZaaktypeDto
            {
                AardRelatie = gerelateerdeZaakType.AardRelatie.ToString(),
                Toelichting = gerelateerdeZaakType.Toelichting,
                ZaakType = _uriService.GetUri(gerelateerdeZaakType.GerelateerdeZaakType),
            };
            gerelateerdeZaakTypen.Add(item);
        }
        dest.GerelateerdeZaakTypen = gerelateerdeZaakTypen;
    }
}

public class MapMergedGerelateerdeZaakTypen : IMappingAction<ZaakType, ZaakTypeRequestDto>
{
    public void Process(ZaakType src, ZaakTypeRequestDto dest, ResolutionContext context)
    {
        var gerelateerdeZaakTypen = new List<GerelateerdeZaaktypeDto>();

        foreach (var gerelateerdeZaakType in src.ZaakTypeGerelateerdeZaakTypen.Where(z => z.GerelateerdeZaakType != null))
        {
            var item = new GerelateerdeZaaktypeDto
            {
                AardRelatie = gerelateerdeZaakType.AardRelatie.ToString(),
                Toelichting = gerelateerdeZaakType.Toelichting,
                ZaakType = gerelateerdeZaakType.GerelateerdeZaakTypeIdentificatie,
            };
            gerelateerdeZaakTypen.Add(item);
        }
        dest.GerelateerdeZaakTypen = gerelateerdeZaakTypen;
    }
}

class MapMergedGerelateerdeZaakTypenUrlBased : IMappingAction<ZaakType, ZaakTypeRequestDto>
{
    private readonly IEntityUriService _uriService;

    public MapMergedGerelateerdeZaakTypenUrlBased(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public void Process(ZaakType src, ZaakTypeRequestDto dest, ResolutionContext context)
    {
        var gerelateerdeZaakTypen = new List<GerelateerdeZaaktypeDto>();

        foreach (var gerelateerdeZaakType in src.ZaakTypeGerelateerdeZaakTypen.Where(z => z.GerelateerdeZaakType != null))
        {
            var item = new GerelateerdeZaaktypeDto
            {
                AardRelatie = gerelateerdeZaakType.AardRelatie.ToString(),
                Toelichting = gerelateerdeZaakType.Toelichting,
                ZaakType = _uriService.GetUri(gerelateerdeZaakType.GerelateerdeZaakType),
            };
            gerelateerdeZaakTypen.Add(item);
        }
        dest.GerelateerdeZaakTypen = gerelateerdeZaakTypen;
    }
}
