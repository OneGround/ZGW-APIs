using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using OneGround.ZGW.Catalogi.Contracts.v1._3;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._3;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<ZaakType, ZaakTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(dest => dest.VersieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VersieDatum)))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus))
            .ForMember(dest => dest.StatusTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<StatusType>>(src => src.StatusTypen))
            .ForMember(dest => dest.RolTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<RolType>>(src => src.RolTypen))
            .ForMember(dest => dest.ResultaatTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<ResultaatType>>(src => src.ResultaatTypen))
            .ForMember(dest => dest.Eigenschappen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<Eigenschap>>(src => src.Eigenschappen))
            .ForMember(dest => dest.VerlengingsTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.VerlengingsTermijn)))
            .ForMember(dest => dest.Servicenorm, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.Servicenorm)))
            .ForMember(dest => dest.Doorlooptijd, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.Doorlooptijd)))
            .ForMember(
                dest => dest.ZaakObjectTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakObjectTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakObjectType>>(src => src.ZaakObjectTypen);
                }
            )
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

        CreateMap<BronCatalogus, BronCatalogusDto>();
        CreateMap<BronZaaktype, BronZaaktypeDto>();

        // Note: This map is used to merge an existing ZAAKTYPE with the PATCH operation
        CreateMap<ZaakType, ZaakTypeRequestDto>()
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(dest => dest.VersieDatum, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.VersieDatum)))
            .ForMember(
                dest => dest.DeelZaakTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakTypeDeelZaakTypen != null);
                    opt.MapFrom(src =>
                        src.ZaakTypeDeelZaakTypen.Where(z => z.DeelZaakType != null).Select(s => s.DeelZaakTypeIdentificatie).Distinct()
                    );
                }
            )
            .ForMember(
                dest => dest.BesluitTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ZaakTypeBesluitTypen != null);
                    opt.MapFrom(src => src.ZaakTypeBesluitTypen.Where(z => z.BesluitType != null).Select(s => s.BesluitTypeOmschrijving).Distinct());
                }
            )
            .AfterMap<MapMergedGerelateerdeZaakTypen>();

        CreateMap<StatusType, StatusTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.ZaakType.Catalogus))
            .ForMember(dest => dest.ZaaktypeIdentificatie, opt => opt.MapFrom(src => src.ZaakType.Identificatie))
            .ForMember(
                dest => dest.Eigenschappen,
                opt =>
                {
                    opt.PreCondition(src => src.StatusTypeVerplichteEigenschappen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<Eigenschap>>(src => src.StatusTypeVerplichteEigenschappen.Select(s => s.Eigenschap));
                }
            )
            .ForMember(dest => dest.CheckListItemStatustypes, opt => opt.MapFrom(src => src.CheckListItemStatustypes))
            .ForMember(dest => dest.OmschrijvingGeneriek, opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.OmschrijvingGeneriek)))
            .ForMember(dest => dest.StatusTekst, opt => opt.MapFrom(src => ProfileHelper.EmptyWhenNull(src.StatusTekst)));

        CreateMap<CheckListItemStatusType, CheckListItemStatusTypeDto>();

        // Note: This map is used to merge an existing STATUSTYPE with the PATCH operation
        CreateMap<StatusType, StatusTypeRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(
                dest => dest.Eigenschappen,
                opt =>
                {
                    opt.PreCondition(src => src.StatusTypeVerplichteEigenschappen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<Eigenschap>>(src => src.StatusTypeVerplichteEigenschappen.Select(s => s.Eigenschap));
                }
            );

        CreateMap<RolType, RolTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(s => s.ZaakType))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.ZaakType.Catalogus))
            .ForMember(dest => dest.ZaaktypeIdentificatie, opt => opt.MapFrom(src => src.ZaakType.Identificatie));

        // Note: This map is used to merge an existing RolType with the PATCH operation
        CreateMap<RolType, RolTypeRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)));

        CreateMap<ZaakTypeInformatieObjectType, ZaakTypeInformatieObjectTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(s => s.ZaakType))
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.InformatieObjectTypeOmschrijving))
            .ForMember(dest => dest.StatusType, opt => opt.MapFrom<MemberUrlResolver, StatusType>(s => s.StatusType))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.ZaakType.Catalogus));

        // Note: This map is used to merge an existing ZaakTypeInformatieObjectTypen with the PATCH operation
        CreateMap<ZaakTypeInformatieObjectType, ZaakTypeInformatieObjectTypeRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.StatusType, opt => opt.MapFrom<MemberUrlResolver, StatusType>(src => src.StatusType))
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.InformatieObjectTypeOmschrijving));

        CreateMap<ResultaatType, ResultaatTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(s => s.ZaakType))
            .ForMember(dest => dest.ArchiefActieTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.ArchiefActieTermijn)))
            .ForMember(dest => dest.ProcesTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.ProcesTermijn)))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.ZaakType.Catalogus))
            .ForMember(dest => dest.ZaaktypeIdentificatie, opt => opt.MapFrom(src => src.ZaakType.Identificatie))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(
                dest => dest.BesluitTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ResultaatTypeBesluitTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<BesluitType>>(src =>
                        src.ResultaatTypeBesluitTypen.Where(b => b.BesluitType != null).Select(b => b.BesluitType)
                    );
                }
            )
            .ForMember(
                dest => dest.BesluittypeOmschrijvingen,
                opt =>
                {
                    opt.PreCondition(src => src.ResultaatTypeBesluitTypen != null);
                    opt.MapFrom(src => src.ResultaatTypeBesluitTypen.Where(b => b.BesluitType != null).Select(b => b.BesluitType.Omschrijving));
                }
            )
            .ForMember(dest => dest.InformatieObjectTypen, opt => opt.MapFrom(src => Enumerable.Empty<string>()))
            .ForMember(dest => dest.InformatieObjectTypeOmschrijvingen, opt => opt.MapFrom(src => Enumerable.Empty<string>()));

        // Note: for PATCH operation
        CreateMap<ResultaatType, ResultaatTypeRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(
                dest => dest.BesluitTypen,
                opt =>
                {
                    opt.PreCondition(src => src.ResultaatTypeBesluitTypen != null);
                    opt.MapFrom(src =>
                        src.ResultaatTypeBesluitTypen.Where(z => z.BesluitType != null).Select(s => s.BesluitTypeOmschrijving).Distinct()
                    );
                }
            );

        CreateMap<Catalogus, CatalogusResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<ZaakType>>(src => src.ZaakTypes))
            .ForMember(dest => dest.BesluitTypen, opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<BesluitType>>(src => src.BesluitTypes))
            .ForMember(
                dest => dest.InformatieObjectTypen,
                opt => opt.MapFrom<MemberUrlsResolver, IEnumerable<InformatieObjectType>>(src => src.InformatieObjectTypes)
            )
            .ForMember(dest => dest.BegindatumVersie, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BegindatumVersie)));

        CreateMap<InformatieObjectType, InformatieObjectTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
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
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus))
            .ForMember(dest => dest.ZaakTypen, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitTypen, opt => opt.Ignore());

        CreateMap<OmschrijvingGeneriek, OmschrijvingGeneriekDto>();

        CreateMap<EigenschapSpecificatie, Catalogi.Contracts.v1.EigenschapSpecificatieDto>();
        CreateMap<Eigenschap, EigenschapResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.StatusType, opt => opt.MapFrom<MemberUrlResolver, StatusType>(src => src.StatusType))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.ZaakType.Catalogus))
            .ForMember(dest => dest.ZaaktypeIdentificatie, opt => opt.MapFrom(src => src.ZaakType.Identificatie))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)));

        // Note: for PATCH operation
        CreateMap<Eigenschap, EigenschapRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.StatusType, opt => opt.MapFrom<MemberUrlResolver, StatusType>(src => src.StatusType))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)));

        CreateMap<BesluitType, BesluitTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
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
            .ForMember(
                dest => dest.ResultaatTypen,
                opt =>
                {
                    opt.PreCondition(src => src.BesluitTypeResultaatTypen != null);
                    opt.MapFrom<MemberUrlsResolver, IEnumerable<ResultaatType>>(src =>
                        src.BesluitTypeResultaatTypen.Where(b => b.ResultaatType != null).Select(b => b.ResultaatType)
                    );
                }
            )
            .ForMember(
                dest => dest.ResultaatTypenOmschrijving,
                opt =>
                {
                    opt.PreCondition(src => src.BesluitTypeResultaatTypen != null);
                    opt.MapFrom(src => src.BesluitTypeResultaatTypen.Where(b => b.ResultaatType != null).Select(b => b.ResultaatType.Omschrijving));
                }
            )
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus))
            .ForMember(dest => dest.ReactieTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.ReactieTermijn)))
            .ForMember(dest => dest.PublicatieTermijn, opt => opt.MapFrom(src => ProfileHelper.Fix0Period(src.PublicatieTermijn)));

        // Note: for PATCH operation
        CreateMap<BesluitType, BesluitTypeRequestDto>()
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(
                dest => dest.ZaakTypen,
                opt =>
                {
                    opt.PreCondition(src => src.BesluitTypeZaakTypen != null);
                    opt.MapFrom(src => src.BesluitTypeZaakTypen.Where(z => z.ZaakType != null).Select(s => s.ZaakTypeIdentificatie).Distinct());
                }
            )
            .ForMember(
                dest => dest.InformatieObjectTypen,
                opt =>
                {
                    opt.PreCondition(src => src.BesluitTypeInformatieObjectTypen != null);
                    opt.MapFrom(src =>
                        src.BesluitTypeInformatieObjectTypen.Where(z => z.InformatieObjectType != null)
                            .Select(s => s.InformatieObjectTypeOmschrijving)
                            .Distinct()
                    );
                }
            )
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.Catalogus));

        CreateMap<ZaakObjectType, ZaakObjectTypeResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom<UrlResolver>())
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)))
            .ForMember(dest => dest.Catalogus, opt => opt.MapFrom<MemberUrlResolver, Catalogus>(src => src.ZaakType.Catalogus))
            .ForMember(dest => dest.ZaaktypeIdentificatie, opt => opt.MapFrom(src => src.ZaakType.Identificatie));
        // TODO: We ask VNG how the relations can be edited:
        //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501
        //.ForMember(dest => dest.ResultaatTypen, opt =>
        //{
        //    opt.PreCondition(src => src.ResultaatTypen != null);
        //    opt.MapFrom<MemberUrlsResolver, IEnumerable<ResultaatType>>(src => src.ResultaatTypen);
        //})
        //.ForMember(dest => dest.StatusTypen, opt =>
        //{
        //    opt.PreCondition(src => src.StatusTypen != null);
        //    opt.MapFrom<MemberUrlsResolver, IEnumerable<StatusType>>(src => src.StatusTypen);
        //});
        // ----

        // Note: This map is used to merge an existing ZAAKOBJECTTYPE with the PATCH operation
        CreateMap<ZaakObjectType, ZaakObjectTypeRequestDto>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom<MemberUrlResolver, ZaakType>(src => src.ZaakType))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.StringDateFromDate(src.EindeObject)));
    }
}

class MapGerelateerdeZaakTypenResponse : IMappingAction<ZaakType, ZaakTypeResponseDto>
{
    private readonly IEntityUriService _uriService;

    public MapGerelateerdeZaakTypenResponse(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public void Process(ZaakType src, ZaakTypeResponseDto dest, ResolutionContext context)
    {
        var gerelateerdeZaakTypen = new List<Catalogi.Contracts.v1.GerelateerdeZaaktypeDto>();

        foreach (var gerelateerdeZaakType in src.ZaakTypeGerelateerdeZaakTypen.Where(z => z.GerelateerdeZaakType != null))
        {
            var item = new Catalogi.Contracts.v1.GerelateerdeZaaktypeDto
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

class MapMergedGerelateerdeZaakTypen : IMappingAction<ZaakType, ZaakTypeRequestDto>
{
    public void Process(ZaakType src, ZaakTypeRequestDto dest, ResolutionContext context)
    {
        var gerelateerdeZaakTypen = new List<Catalogi.Contracts.v1.GerelateerdeZaaktypeDto>();

        foreach (var gerelateerdeZaakType in src.ZaakTypeGerelateerdeZaakTypen.Where(z => z.GerelateerdeZaakType != null))
        {
            var item = new Catalogi.Contracts.v1.GerelateerdeZaaktypeDto
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
