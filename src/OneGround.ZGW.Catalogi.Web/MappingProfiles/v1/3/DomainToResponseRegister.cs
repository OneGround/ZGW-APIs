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
            // ZaakTypeResponseDto declares `public IEnumerable<string> ZaakObjectTypen { get; set; } = [];`
            // (Contracts/v1/3/Responses/ZaakTypeResponseDto.cs:13) -- unlike every other PreCondition-folded
            // member on this config (StatusTypen/ResultaatTypen/Eigenschappen/InformatieObjectTypen/RolTypen
            // all have no initializer), a null navigation collection here must fold to
            // Enumerable.Empty<string>(), not null, or the API response silently changes from
            // "zaakobjecttypen": [] to "zaakobjecttypen": null for the normal case of a ZaakType with zero
            // linked ZaakObjectTypen.
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

                    // These three folds must run here, in .AfterMapping, not in a plain .Map(...)
                    // projection: the global EmptyCollectionIfNull destination transform (registered in
                    // AddZgwMapster, for parity with AutoMapper's AllowNullCollections=false default)
                    // re-coalesces ANY null returned from an explicit .Map(...) lambda into an empty
                    // collection -- including this PreCondition-emulating null, which is supposed to
                    // reproduce AutoMapper's real "member left completely untouched" behavior (these
                    // three destination properties have no field initializer, so untouched means null,
                    // not empty). .AfterMapping runs after that transform pipeline has already applied,
                    // so a plain assignment made here is not re-coalesced. Contrast with ZaakObjectTypen
                    // above and InformatieObjectTypeResponseDto.ZaakTypen/BesluitTypen below, whose
                    // destination DOES have a `= []` initializer -- there, folding to
                    // Enumerable.Empty<string>() in a plain .Map(...) is correct (and the transform
                    // coalescing it again is harmless).
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
            // Note: unlike MapGerelateerdeZaakTypenResponse above, this port of MapMergedGerelateerdeZaakTypen
            // needs no IEntityUriService/URL lookup -- it uses the already-denormalized
            // GerelateerdeZaakTypeIdentificatie string field directly -- and, critically, does NOT filter out
            // entries with a null GerelateerdeZaakType navigation (no .Where(...), unlike the Response version
            // above): it iterates every item in src.ZaakTypeGerelateerdeZaakTypen unconditionally, matching the
            // original AutoMapper IMappingAction body exactly.
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

                    // See the long comment on the ZaakType->ZaakTypeResponseDto config above: these folds
                    // must run in .AfterMapping, not a plain .Map(...), to avoid the global
                    // EmptyCollectionIfNull destination transform re-coalescing a deliberate null (these
                    // destination properties have no field initializer) into an empty collection.
                    dest.DeelZaakTypen =
                        src.ZaakTypeDeelZaakTypen == null ? null : src.ZaakTypeDeelZaakTypen.Select(s => s.DeelZaakTypeIdentificatie).Distinct();
                    dest.BesluitTypen =
                        src.ZaakTypeBesluitTypen == null ? null : src.ZaakTypeBesluitTypen.Select(s => s.BesluitTypeOmschrijving).Distinct();
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
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
            .Map(dest => dest.ZaaktypeIdentificatie, src => src.ZaakType.Identificatie)
            .Ignore(dest => dest.Eigenschappen)
            // Must run in .AfterMapping, not a plain .Map(...): see the long comment on the
            // ZaakType->ZaakTypeResponseDto config above (global EmptyCollectionIfNull transform
            // re-coalesces a deliberate null into an empty collection when returned from .Map(...)).
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
            // See the long comment on the ZaakType->ZaakTypeResponseDto config above: must run in
            // .AfterMapping, not a plain .Map(...), to avoid the global EmptyCollectionIfNull transform
            // re-coalescing a deliberate null into an empty collection.
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
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
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
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus));

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
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
            .Map(dest => dest.ZaaktypeIdentificatie, src => src.ZaakType.Identificatie)
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.StringDateFromDate(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.StringDateFromDate(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.StringDateFromDate(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.StringDateFromDate(src.EindeObject))
            .Ignore(dest => dest.BesluitTypen)
            .Ignore(dest => dest.BesluittypeOmschrijvingen)
            // Note: preserved verbatim from the AutoMapper source -- these two members are not a
            // PreCondition-fold at all. The original always maps them to a bare Enumerable.Empty<string>(),
            // unconditionally and regardless of source data (presumably a not-yet-implemented feature) -- do
            // not "improve" this by wiring up real source data.
            .Map(dest => dest.InformatieObjectTypen, src => Enumerable.Empty<string>())
            .Map(dest => dest.InformatieObjectTypeOmschrijvingen, src => Enumerable.Empty<string>())
            // BesluitTypen/BesluittypeOmschrijvingen must run in .AfterMapping, not a plain .Map(...):
            // see the long comment on the ZaakType->ZaakTypeResponseDto config above (global
            // EmptyCollectionIfNull transform re-coalesces a deliberate null into an empty collection).
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
                            : src.ResultaatTypeBesluitTypen.Where(b => b.BesluitType != null).Select(b => b.BesluitType.Omschrijving);
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
            // Must run in .AfterMapping, not a plain .Map(...): see the long comment on the
            // ZaakType->ZaakTypeResponseDto config above (global EmptyCollectionIfNull transform
            // re-coalesces a deliberate null into an empty collection when returned from .Map(...)).
            .AfterMapping(
                (src, dest) =>
                    dest.BesluitTypen =
                        src.ResultaatTypeBesluitTypen == null
                            ? null
                            : src.ResultaatTypeBesluitTypen.Where(z => z.BesluitType != null).Select(s => s.BesluitTypeOmschrijving).Distinct()
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
            // InformatieObjectTypeDto (v1._3) initializes ZaakTypen/BesluitTypen with `= []` in its base
            // class (Contracts/v1/3/InformatieObjectTypeDto.cs:40,43) -- the same bug class as the v1
            // InformatieObjectTypeResponseDto fix in the sibling DomainToResponseRegister.cs, but a
            // different (v1._3-namespaced) DTO with its own separate `= []` base. A null navigation
            // collection must fold to Enumerable.Empty<string>(), not null, or the API response silently
            // changes from "zaaktypen"/"besluittypen": [] to null for the normal case of an
            // InformatieObjectType with zero linked relations.
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

        config.NewConfig<EigenschapSpecificatie, Catalogi.Contracts.v1.EigenschapSpecificatieDto>();

        config
            .NewConfig<Eigenschap, EigenschapResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.ZaakType, src => MapsterUrlResolver.ResolveUrl(src.ZaakType))
            .Map(dest => dest.StatusType, src => MapsterUrlResolver.ResolveUrl(src.StatusType))
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
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
            // These five folds must run in .AfterMapping, not a plain .Map(...): see the long comment on
            // the ZaakType->ZaakTypeResponseDto config above (global EmptyCollectionIfNull transform
            // re-coalesces a deliberate null into an empty collection when returned from .Map(...)).
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
                            : src.BesluitTypeResultaatTypen.Where(b => b.ResultaatType != null).Select(b => b.ResultaatType.Omschrijving);
                    dest.VastgelegdIn =
                        src.BesluitTypeInformatieObjectTypen == null
                            ? null
                            : src
                                .BesluitTypeInformatieObjectTypen.Where(b => b.InformatieObjectType != null)
                                .Select(b => b.InformatieObjectType.Omschrijving)
                                .Distinct();
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
            // Must run in .AfterMapping, not a plain .Map(...): see the long comment on the
            // ZaakType->ZaakTypeResponseDto config above (global EmptyCollectionIfNull transform
            // re-coalesces a deliberate null into an empty collection when returned from .Map(...)).
            .AfterMapping(
                (src, dest) =>
                {
                    dest.ZaakTypen =
                        src.BesluitTypeZaakTypen == null
                            ? null
                            : src.BesluitTypeZaakTypen.Where(z => z.ZaakType != null).Select(s => s.ZaakTypeIdentificatie).Distinct();
                    dest.InformatieObjectTypen =
                        src.BesluitTypeInformatieObjectTypen == null
                            ? null
                            : src.BesluitTypeInformatieObjectTypen.Select(s => s.InformatieObjectTypeOmschrijving).Distinct();
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
            .Map(dest => dest.Catalogus, src => MapsterUrlResolver.ResolveUrl(src.ZaakType.Catalogus))
            .Map(dest => dest.ZaaktypeIdentificatie, src => src.ZaakType.Identificatie);
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
