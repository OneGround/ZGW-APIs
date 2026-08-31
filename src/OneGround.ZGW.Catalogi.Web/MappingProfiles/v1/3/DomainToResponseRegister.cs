using System.Collections.Generic;
using System.Linq;
using Mapster;
using OneGround.ZGW.Catalogi.Contracts.v1._3;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._3;

/// <summary>
/// Serves the audit trail (<c>IZgwMapper</c>) and the PATCH merge (<c>IZgwRequestMerger</c>), not just
/// the controllers.
/// </summary>
/// <remarks>
/// Three rules govern this file; the first is documented on the sibling
/// <see cref="MappingProfiles.v1.DomainToResponseRegister"/> and this file has far more of those folds.
/// <list type="bullet">
/// <item>Empty-vs-null on a folded collection member — see the sibling register.</item>
/// <item>Every <c>.AfterMapping</c> fold ends in <c>.ToList()</c>: keep it. A bare LINQ chain hands the
/// DTO a deferred query over the source's live navigation collection, and some update handlers
/// <c>Clear()</c> those collections after the DTO exists. <c>ResolveUrls</c> already materializes;
/// only folds projecting denormalized string fields need the explicit call.</item>
/// <item><c>src.ZaakType == null ? null : ...</c> on <c>dest.Catalogus</c>: a member path is guarded
/// only where it is a METHOD ARGUMENT. A bare <c>src.ZaakType.Identificatie</c> null-propagates on its
/// own; <c>ResolveUrl(src.ZaakType.Catalogus)</c> does not. <c>?.</c> is CS8072 in an expression tree,
/// hence the ternary.</item>
/// </list>
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
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Map(dest => dest.VersieDatum, src => ProfileHelper.StringDateFromDate(src.VersieDatum))
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            .Map(dest => dest.StatusTypen, src => MapsterUrlResolver.ResolveUrls(src.StatusTypen))
            .Map(dest => dest.RolTypen, src => MapsterUrlResolver.ResolveUrls(src.RolTypen))
            .Map(dest => dest.ResultaatTypen, src => MapsterUrlResolver.ResolveUrls(src.ResultaatTypen))
            .Map(dest => dest.Eigenschappen, src => MapsterUrlResolver.ResolveUrls(src.Eigenschappen))
            .Map(dest => dest.VerlengingsTermijn, src => ProfileHelper.Fix0Period(src.VerlengingsTermijn))
            .Map(dest => dest.Servicenorm, src => ProfileHelper.Fix0Period(src.Servicenorm))
            .Map(dest => dest.Doorlooptijd, src => ProfileHelper.Fix0Period(src.Doorlooptijd))
            // Fold -> empty: ZaakObjectTypen is the one member here with a `= []` initializer.
            .Map(
                dest => dest.ZaakObjectTypen,
                src => src.ZaakObjectTypen == null ? Enumerable.Empty<string>() : MapsterUrlResolver.ResolveUrls(src.ZaakObjectTypen)
            )
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
                        .Select(z => new Catalogi.Contracts.v1.GerelateerdeZaaktypeDto
                        {
                            AardRelatie = z.AardRelatie.ToString(),
                            Toelichting = z.Toelichting,
                            ZaakType = uriService.GetUri(z.GerelateerdeZaakType),
                        })
                        .ToList();

                    // Must stay in .AfterMapping to survive as null -- see the class remarks.
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

        config.NewConfig<BronCatalogus, BronCatalogusDto>();
        config.NewConfig<BronZaaktype, BronZaaktypeDto>();

        // Note: This map is used to merge an existing ZAAKTYPE with the PATCH operation
        config
            .NewConfig<ZaakType, ZaakTypeRequestDto>()
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Map(dest => dest.VersieDatum, src => ProfileHelper.StringDateFromDate(src.VersieDatum))
            .Ignore(dest => dest.GerelateerdeZaakTypen)
            .Ignore(dest => dest.DeelZaakTypen)
            .Ignore(dest => dest.BesluitTypen)
            // Deliberately does NOT filter out entries with a null GerelateerdeZaakType, unlike the
            // response map above: this one reads the denormalized identificatie, not a URL.
            .AfterMapping(
                (src, dest) =>
                {
                    var gerelateerdeZaakTypen = new List<Catalogi.Contracts.v1.GerelateerdeZaaktypeDto>();

                    foreach (var gerelateerdeZaakType in src.ZaakTypeGerelateerdeZaakTypen)
                    {
                        gerelateerdeZaakTypen.Add(
                            new Catalogi.Contracts.v1.GerelateerdeZaaktypeDto
                            {
                                AardRelatie = gerelateerdeZaakType.AardRelatie.ToString(),
                                Toelichting = gerelateerdeZaakType.Toelichting,
                                ZaakType = gerelateerdeZaakType.GerelateerdeZaakTypeIdentificatie,
                            }
                        );
                    }
                    dest.GerelateerdeZaakTypen = gerelateerdeZaakTypen;

                    // Must stay in .AfterMapping to survive as null -- see the class remarks.
                    dest.DeelZaakTypen =
                        src.ZaakTypeDeelZaakTypen == null
                            ? null
                            : src.ZaakTypeDeelZaakTypen.Select(s => s.DeelZaakTypeIdentificatie).Distinct().ToList();
                    dest.BesluitTypen =
                        src.ZaakTypeBesluitTypen == null ? null : src.ZaakTypeBesluitTypen.Select(s => s.BesluitTypeOmschrijving).Distinct().ToList();
                }
            );

        config
            .NewConfig<StatusType, StatusTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Map(dest => dest.Catalogus, src => src.ZaakType == null ? null : MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
            .Map(dest => dest.ZaaktypeIdentificatie, src => src.ZaakType.Identificatie)
            .Ignore(dest => dest.Eigenschappen)
            // Must stay in .AfterMapping to survive as null -- see the class remarks.
            .AfterMapping(
                (src, dest) =>
                    dest.Eigenschappen =
                        src.StatusTypeVerplichteEigenschappen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(src.StatusTypeVerplichteEigenschappen.Select(s => s.Eigenschap))
            )
            .Map(dest => dest.CheckListItemStatustypes, src => src.CheckListItemStatustypes)
            .Map(dest => dest.OmschrijvingGeneriek, src => ProfileHelper.EmptyWhenNull(src.OmschrijvingGeneriek))
            .Map(dest => dest.StatusTekst, src => ProfileHelper.EmptyWhenNull(src.StatusTekst));

        config.NewConfig<CheckListItemStatusType, CheckListItemStatusTypeDto>();

        // Note: This map is used to merge an existing STATUSTYPE with the PATCH operation
        config
            .NewConfig<StatusType, StatusTypeRequestDto>()
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Ignore(dest => dest.Eigenschappen)
            // Must stay in .AfterMapping to survive as null -- see the class remarks.
            .AfterMapping(
                (src, dest) =>
                    dest.Eigenschappen =
                        src.StatusTypeVerplichteEigenschappen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(src.StatusTypeVerplichteEigenschappen.Select(s => s.Eigenschap))
            );

        config
            .NewConfig<RolType, RolTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Map(dest => dest.Catalogus, src => src.ZaakType == null ? null : MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
            .Map(dest => dest.ZaaktypeIdentificatie, src => src.ZaakType.Identificatie);

        // Note: This map is used to merge an existing RolType with the PATCH operation
        config
            .NewConfig<RolType, RolTypeRequestDto>()
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject));

        config
            .NewConfig<ZaakTypeInformatieObjectType, ZaakTypeInformatieObjectTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.InformatieObjectType, src => src.InformatieObjectTypeOmschrijving)
            .Map(dest => dest.StatusType, src => MapsterUrlResolver.ResolveUrl(src.StatusType))
            .Map(dest => dest.Catalogus, src => src.ZaakType == null ? null : MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus));

        // Note: This map is used to merge an existing ZaakTypeInformatieObjectTypen with the PATCH operation
        config
            .NewConfig<ZaakTypeInformatieObjectType, ZaakTypeInformatieObjectTypeRequestDto>()
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.StatusType, src => MapsterUrlResolver.ResolveUrl(src.StatusType))
            .Map(dest => dest.InformatieObjectType, src => src.InformatieObjectTypeOmschrijving);

        config
            .NewConfig<ResultaatType, ResultaatTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.ArchiefActieTermijn, src => ProfileHelper.Fix0Period(src.ArchiefActieTermijn))
            .Map(dest => dest.ProcesTermijn, src => ProfileHelper.Fix0Period(src.ProcesTermijn))
            .Map(dest => dest.Catalogus, src => src.ZaakType == null ? null : MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
            .Map(dest => dest.ZaaktypeIdentificatie, src => src.ZaakType.Identificatie)
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Ignore(dest => dest.BesluitTypen)
            .Ignore(dest => dest.BesluittypeOmschrijvingen)
            // Always empty regardless of source data, as before -- not a fold. Do not "improve" this by
            // wiring up real source data.
            .Map(dest => dest.InformatieObjectTypen, src => Enumerable.Empty<string>())
            .Map(dest => dest.InformatieObjectTypeOmschrijvingen, src => Enumerable.Empty<string>())
            // Must stay in .AfterMapping to survive as null -- see the class remarks.
            .AfterMapping(
                (src, dest) =>
                {
                    dest.BesluitTypen =
                        src.ResultaatTypeBesluitTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(
                                src.ResultaatTypeBesluitTypen.Where(b => b.BesluitType != null).Select(b => b.BesluitType)
                            );
                    dest.BesluittypeOmschrijvingen =
                        src.ResultaatTypeBesluitTypen == null
                            ? null
                            : src.ResultaatTypeBesluitTypen.Where(b => b.BesluitType != null).Select(b => b.BesluitType.Omschrijving).ToList();
                }
            );

        // Note: for PATCH operation
        config
            .NewConfig<ResultaatType, ResultaatTypeRequestDto>()
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Ignore(dest => dest.BesluitTypen)
            // No source member, so this stays null -- the asymmetry with the response map above, which
            // pins the same-named member to empty, is deliberate and pre-existing.
            .Ignore(dest => dest.InformatieObjectTypen)
            // Must stay in .AfterMapping to survive as null -- see the class remarks.
            .AfterMapping(
                (src, dest) =>
                    dest.BesluitTypen =
                        src.ResultaatTypeBesluitTypen == null
                            ? null
                            : src
                                .ResultaatTypeBesluitTypen.Where(z => z.BesluitType != null)
                                .Select(s => s.BesluitTypeOmschrijving)
                                .Distinct()
                                .ToList()
            );

        config
            .NewConfig<Catalogus, CatalogusResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakTypen, src => MapsterUrlResolver.ResolveUrls(src.ZaakTypes))
            .Map(dest => dest.BesluitTypen, src => MapsterUrlResolver.ResolveUrls(src.BesluitTypes))
            .Map(dest => dest.InformatieObjectTypen, src => MapsterUrlResolver.ResolveUrls(src.InformatieObjectTypes))
            .Map(dest => dest.BegindatumVersie, src => ProfileHelper.StringDateFromDate(src.BegindatumVersie));

        config
            .NewConfig<InformatieObjectType, InformatieObjectTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            // Fold -> empty: the v1._3 DTO initializes both with `= []` in its base class.
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
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            .Ignore(dest => dest.ZaakTypen)
            .Ignore(dest => dest.BesluitTypen);

        config.NewConfig<OmschrijvingGeneriek, OmschrijvingGeneriekDto>();

        // EigenschapSpecificatie -> v1.EigenschapSpecificatieDto is absent on purpose: v1.3 reuses the v1
        // DTO, so it is the same CLR pair the v1 register already owns. Declaring it twice would replace it.

        config
            .NewConfig<Eigenschap, EigenschapResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.StatusType, src => MapsterUrlResolver.ResolveUrl(src.StatusType))
            .Map(dest => dest.Catalogus, src => src.ZaakType == null ? null : MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
            .Map(dest => dest.ZaaktypeIdentificatie, src => src.ZaakType.Identificatie)
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject));

        // Note: for PATCH operation
        config
            .NewConfig<Eigenschap, EigenschapRequestDto>()
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.StatusType, src => MapsterUrlResolver.ResolveUrl(src.StatusType))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject));

        config
            .NewConfig<BesluitType, BesluitTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Ignore(dest => dest.ZaakTypen)
            .Ignore(dest => dest.InformatieObjectTypen)
            .Ignore(dest => dest.ResultaatTypen)
            .Ignore(dest => dest.ResultaatTypenOmschrijving)
            .Ignore(dest => dest.VastgelegdIn)
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            .Map(dest => dest.ReactieTermijn, src => ProfileHelper.Fix0Period(src.ReactieTermijn))
            .Map(dest => dest.PublicatieTermijn, src => ProfileHelper.Fix0Period(src.PublicatieTermijn))
            // Must stay in .AfterMapping to survive as null -- see the class remarks.
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
                    dest.ResultaatTypen =
                        src.BesluitTypeResultaatTypen == null
                            ? null
                            : MapsterUrlResolver.ResolveUrls(
                                src.BesluitTypeResultaatTypen.Where(b => b.ResultaatType != null).Select(b => b.ResultaatType)
                            );
                    dest.ResultaatTypenOmschrijving =
                        src.BesluitTypeResultaatTypen == null
                            ? null
                            : src.BesluitTypeResultaatTypen.Where(b => b.ResultaatType != null).Select(b => b.ResultaatType.Omschrijving).ToList();
                    dest.VastgelegdIn =
                        src.BesluitTypeInformatieObjectTypen == null
                            ? null
                            : src
                                .BesluitTypeInformatieObjectTypen.Where(b => b.InformatieObjectType != null)
                                .Select(b => b.InformatieObjectType.Omschrijving)
                                .Distinct()
                                .ToList();
                }
            );

        // Note: for PATCH operation
        config
            .NewConfig<BesluitType, BesluitTypeRequestDto>()
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Ignore(dest => dest.ZaakTypen)
            .Ignore(dest => dest.InformatieObjectTypen)
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.Catalogus))
            // Must stay in .AfterMapping to survive as null -- see the class remarks.
            .AfterMapping(
                (src, dest) =>
                {
                    dest.ZaakTypen =
                        src.BesluitTypeZaakTypen == null
                            ? null
                            : src.BesluitTypeZaakTypen.Where(z => z.ZaakType != null).Select(s => s.ZaakTypeIdentificatie).Distinct().ToList();
                    dest.InformatieObjectTypen =
                        src.BesluitTypeInformatieObjectTypen == null
                            ? null
                            : src.BesluitTypeInformatieObjectTypen.Select(s => s.InformatieObjectTypeOmschrijving).Distinct().ToList();
                }
            );

        config
            .NewConfig<ZaakObjectType, ZaakObjectTypeResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Map(dest => dest.Catalogus, src => src.ZaakType == null ? null : MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
            .Map(dest => dest.ZaaktypeIdentificatie, src => src.ZaakType.Identificatie)
            // No source member yet -- the feeding navigations are commented out on the entity pending
            // the VNG question below. Unignore together with the two .Map calls once answered.
            .Ignore(dest => dest.ResultaatTypen)
            .Ignore(dest => dest.StatusTypen);
        // TODO: We ask VNG how the relations can be edited:
        //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501
        //.Map(dest => dest.ResultaatTypen, src => src.ResultaatTypen == null ? null : MapsterUrlResolver.ResolveUrls(src.ResultaatTypen))
        //.Map(dest => dest.StatusTypen, src => src.StatusTypen == null ? null : MapsterUrlResolver.ResolveUrls(src.StatusTypen));
        // ----

        // Note: This map is used to merge an existing ZAAKOBJECTTYPE with the PATCH operation
        config
            .NewConfig<ZaakObjectType, ZaakObjectTypeRequestDto>()
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject));
    }
}
