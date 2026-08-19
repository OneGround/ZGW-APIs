using System.Linq;
using Mapster;
using OneGround.ZGW.Catalogi.Contracts.v1;
using OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1;

/// <summary>
/// Port of the v1 AutoMapper <c>DomainToResponseProfile</c>. Also serves the audit trail
/// (<c>IZgwMapper</c>) and the PATCH merge (<c>IZgwRequestMerger</c>), not just the controllers.
/// </summary>
/// <remarks>
/// <para><b>Emulating AutoMapper's <c>PreCondition</c> — the one rule to know before editing.</b>
/// AutoMapper skipped the whole member assignment when a <c>PreCondition</c> was false, leaving the
/// destination at whatever its field initializer gave it. Mapster has no equivalent, so each of those
/// members folds its null-check into the projection, and the fold has to reproduce the right default:
/// </para>
/// <list type="bullet">
/// <item>Destination property HAS a <c>= []</c> initializer → fold to <c>Enumerable.Empty&lt;string&gt;()</c>
/// in a plain <c>.Map(...)</c>. (e.g. <c>InformatieObjectTypeResponseDto.ZaakTypen/BesluitTypen</c>.)</item>
/// <item>Destination property has NO initializer → must produce <c>null</c>, and the assignment must live
/// in <c>.AfterMapping</c>, never in a <c>.Map(...)</c> lambda. <c>AddZgwMapster</c> registers
/// <c>DestinationTransform.EmptyCollectionIfNull</c> globally (for parity with AutoMapper's
/// <c>AllowNullCollections = false</c>), and it re-coalesces ANY null a <c>.Map(...)</c> lambda returns
/// into an empty collection — including the deliberate one. <c>.AfterMapping</c> runs after that
/// transform pipeline, so a plain assignment there survives as null.</item>
/// </list>
/// <para>Check the destination property's own initializer before adding or moving a fold; getting it
/// wrong silently changes a JSON response from <c>[]</c> to <c>null</c> or back, and changes every audit
/// record with it. Guarded by the <c>*_maps_to_null</c> facts in the mapping tests, which only
/// discriminate because they run on the real seam config — see <c>ZtcMapperTestHost</c>.</para>
/// </remarks>
public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<ZaakType, ZaakTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.VersieDatum, src => ProfileHelper.StringDateFromDate(src.VersieDatum))
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            .Map(dest => dest.StatusTypen, src => MapsterUrlResolver.ResolveUrls(src.StatusTypen))
            .Map(dest => dest.RolTypen, src => MapsterUrlResolver.ResolveUrls(src.RolTypen))
            .Map(dest => dest.ResultaatTypen, src => MapsterUrlResolver.ResolveUrls(src.ResultaatTypen))
            .Map(dest => dest.Eigenschappen, src => MapsterUrlResolver.ResolveUrls(src.Eigenschappen))
            .Map(dest => dest.VerlengingsTermijn, src => ProfileHelper.Fix0Period(src.VerlengingsTermijn))
            .Map(dest => dest.Servicenorm, src => ProfileHelper.Fix0Period(src.Servicenorm))
            .Map(dest => dest.Doorlooptijd, src => ProfileHelper.Fix0Period(src.Doorlooptijd))
            .Ignore(dest => dest.GerelateerdeZaakTypen)
            .Ignore(dest => dest.InformatieObjectTypen)
            .Ignore(dest => dest.DeelZaakTypen)
            .Ignore(dest => dest.BesluitTypen)
            .AfterMapping(
                (src, dest) =>
                {
                    var uriService = MapContext.Current.GetService<IEntityUriService>();
                    dest.GerelateerdeZaakTypen = src
                        .ZaakTypeGerelateerdeZaakTypen.Where(z => z.GerelateerdeZaakType != null)
                        .Select(z => new GerelateerdeZaaktypeDto
                        {
                            AardRelatie = z.AardRelatie.ToString(),
                            Toelichting = z.Toelichting,
                            ZaakType = uriService.GetUri(z.GerelateerdeZaakType),
                        })
                        .ToList();

                    // PreCondition folds -> null. No initializer on these three, and .AfterMapping is
                    // required so EmptyCollectionIfNull cannot re-coalesce it. See the class remarks.
                    dest.InformatieObjectTypen =
                        src.ZaakTypeInformatieObjectTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(
                                src.ZaakTypeInformatieObjectTypen.Where(i => i.InformatieObjectType != null).Select(s => s.InformatieObjectType)
                            );
                    dest.DeelZaakTypen =
                        src.ZaakTypeDeelZaakTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(
                                src.ZaakTypeDeelZaakTypen.Where(z => z.DeelZaakType != null).Select(s => s.DeelZaakType)
                            );
                    dest.BesluitTypen =
                        src.ZaakTypeBesluitTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(src.ZaakTypeBesluitTypen.Where(b => b.BesluitType != null).Select(b => b.BesluitType));
                }
            );

        config.NewConfig<ReferentieProces, ReferentieProcesDto>();

        // Note: This map is used to merge an existing ZAAKTYPE with the PATCH operation
        config
            .NewConfig<ZaakType, ZaakTypeRequestDto>()
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.VersieDatum, src => ProfileHelper.StringDateFromDate(src.VersieDatum))
            .Ignore(dest => dest.GerelateerdeZaakTypen)
            .Ignore(dest => dest.DeelZaakTypen)
            .Ignore(dest => dest.BesluitTypen)
            .AfterMapping(
                (src, dest) =>
                {
                    var uriService = MapContext.Current.GetService<IEntityUriService>();
                    dest.GerelateerdeZaakTypen = src
                        .ZaakTypeGerelateerdeZaakTypen.Where(z => z.GerelateerdeZaakType != null)
                        .Select(z => new GerelateerdeZaaktypeDto
                        {
                            AardRelatie = z.AardRelatie.ToString(),
                            Toelichting = z.Toelichting,
                            ZaakType = uriService.GetUri(z.GerelateerdeZaakType),
                        })
                        .ToList();

                    // PreCondition folds -> null; no initializer, so .AfterMapping. See the class remarks.
                    dest.DeelZaakTypen =
                        src.ZaakTypeDeelZaakTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(
                                src.ZaakTypeDeelZaakTypen.Where(z => z.DeelZaakType != null).Select(s => s.DeelZaakType)
                            );
                    dest.BesluitTypen =
                        src.ZaakTypeBesluitTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(src.ZaakTypeBesluitTypen.Where(b => b.BesluitType != null).Select(b => b.BesluitType));
                }
            );

        config
            .NewConfig<StatusType, StatusTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.OmschrijvingGeneriek, src => ProfileHelper.EmptyWhenNull(src.OmschrijvingGeneriek))
            .Map(dest => dest.StatusTekst, src => ProfileHelper.EmptyWhenNull(src.StatusTekst));

        // Note: This map is used to merge an existing STATUSTYPE with the PATCH operation
        config.NewConfig<StatusType, StatusTypeRequestDto>().Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType));

        config
            .NewConfig<RolType, RolTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType));

        // Note: This map is used to merge an existing RolType with the PATCH operation
        config.NewConfig<RolType, RolTypeRequestDto>().Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType));

        config
            .NewConfig<ZaakTypeInformatieObjectType, ZaakTypeInformatieObjectTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.StatusType, src => MapsterUrlResolver.ResolveUrl(src.StatusType))
            .Map(dest => dest.InformatieObjectType, src => MapsterUrlResolver.ResolveUrl(src.InformatieObjectType));

        // Note: This map is used to merge an existing ZaakTypeInformatieObjectTypen with the PATCH operation
        config
            .NewConfig<ZaakTypeInformatieObjectType, ZaakTypeInformatieObjectTypeRequestDto>()
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.StatusType, src => MapsterUrlResolver.ResolveUrl(src.StatusType))
            .Map(dest => dest.InformatieObjectType, src => MapsterUrlResolver.ResolveUrl(src.InformatieObjectType));

        config
            .NewConfig<ResultaatType, ResultaatTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.ArchiefActieTermijn, src => ProfileHelper.Fix0Period(src.ArchiefActieTermijn));

        // Note: for PATCH operation
        config.NewConfig<ResultaatType, ResultaatTypeRequestDto>().Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType));

        config
            .NewConfig<BronDatumArchiefProcedure, BronDatumArchiefProcedureDto>()
            .Map(dest => dest.ProcesTermijn, src => ProfileHelper.Fix0Period(src.ProcesTermijn));

        config
            .NewConfig<Catalogus, CatalogusResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakTypen, src => MapsterUrlResolver.ResolveUrls(src.ZaakTypes))
            .Map(dest => dest.BesluitTypen, src => MapsterUrlResolver.ResolveUrls(src.BesluitTypes))
            .Map(dest => dest.InformatieObjectTypen, src => MapsterUrlResolver.ResolveUrls(src.InformatieObjectTypes));

        config
            .NewConfig<InformatieObjectType, InformatieObjectTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            // PreCondition folds -> empty: InformatieObjectTypeDto initializes both with `= []`, so a
            // plain .Map is correct here. See the class remarks.
            .Map(
                dest => dest.ZaakTypen,
                src =>
                    src.InformatieObjectTypeZaakTypen == null
                        ? Enumerable.Empty<string>()
                        : MapsterUrlResolver.ResolveUrls(src.InformatieObjectTypeZaakTypen.Where(z => z.ZaakType != null).Select(b => b.ZaakType))
            )
            .Map(
                dest => dest.BesluitTypen,
                src =>
                    src.InformatieObjectTypeBesluitTypen == null
                        ? Enumerable.Empty<string>()
                        : MapsterUrlResolver.ResolveUrls(
                            src.InformatieObjectTypeBesluitTypen.Where(z => z.BesluitType != null).Select(b => b.BesluitType)
                        )
            );

        // Note: for PATCH operation
        config
            .NewConfig<InformatieObjectType, InformatieObjectTypeRequestDto>()
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            .Ignore(dest => dest.ZaakTypen)
            .Ignore(dest => dest.BesluitTypen);

        config.NewConfig<EigenschapSpecificatie, EigenschapSpecificatieDto>();
        config
            .NewConfig<Eigenschap, EigenschapResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType));

        // Note: for PATCH operation
        config.NewConfig<Eigenschap, EigenschapRequestDto>().Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType));

        config
            .NewConfig<BesluitType, BesluitTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Ignore(dest => dest.ZaakTypen)
            .Ignore(dest => dest.InformatieObjectTypen)
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            .Map(dest => dest.ReactieTermijn, src => ProfileHelper.Fix0Period(src.ReactieTermijn))
            .Map(dest => dest.PublicatieTermijn, src => ProfileHelper.Fix0Period(src.PublicatieTermijn))
            // PreCondition folds -> null; no initializer, so .AfterMapping. See the class remarks.
            .AfterMapping(
                (src, dest) =>
                {
                    dest.ZaakTypen =
                        src.BesluitTypeZaakTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(src.BesluitTypeZaakTypen.Where(b => b.ZaakType != null).Select(b => b.ZaakType));
                    dest.InformatieObjectTypen =
                        src.BesluitTypeInformatieObjectTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(
                                src.BesluitTypeInformatieObjectTypen.Where(b => b.InformatieObjectType != null).Select(b => b.InformatieObjectType)
                            );
                }
            );

        // Note: for PATCH operation
        config
            .NewConfig<BesluitType, BesluitTypeRequestDto>()
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Ignore(dest => dest.ZaakTypen)
            .Ignore(dest => dest.InformatieObjectTypen)
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            .AfterMapping(
                (src, dest) =>
                {
                    dest.ZaakTypen =
                        src.BesluitTypeZaakTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(src.BesluitTypeZaakTypen.Where(b => b.ZaakType != null).Select(b => b.ZaakType));
                    dest.InformatieObjectTypen =
                        src.BesluitTypeInformatieObjectTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(
                                src.BesluitTypeInformatieObjectTypen.Where(b => b.InformatieObjectType != null).Select(b => b.InformatieObjectType)
                            );
                }
            );
    }
}
