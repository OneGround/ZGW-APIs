using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Services.UriServices;
using V3 = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using V4 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._4;

/// <summary>
/// The mapping of the entities to the v1.4 responses. A v1.4 response is the v1.3 response plus <c>_expand</c> (filled by the expand engine, not by
/// the mapping), so each mapping inherits the v1.3 one and only ignores <c>_expand</c>.
/// <para>
/// This is deliberately not an <c>IRegister</c>: <c>Inherits</c> copies the v1.3 mapping at the moment it is called, and the order in which Mapster
/// scans the registers is undefined, so a register of its own could silently inherit nothing. It is registered by <c>ResponseMappingsRegister</c>,
/// right after the v1.3 mapping that it inherits.
/// </para>
/// <para>
/// Besides <c>_expand</c>, v1.4 adds a number of read-only, denormalized lists (omschrijvingen and identificaties of related types). They have no source
/// member of their own, so each is ignored in the convention mapping and filled in an <c>.AfterMapping</c>, which runs after the inherited v1.3
/// <c>.AfterMapping</c>. A navigation that is not loaded (<c>null</c>) gives an empty list, never <c>null</c>.
/// </para>
/// </summary>
internal static class DomainToResponseMappings
{
    public static void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Catalogus, V4.CatalogusResponseDto>()
            .Inherits<Catalogus, V3.CatalogusResponseDto>()
            .Ignore(dest => dest.Expand)
            .Ignore(dest => dest.BesluitTypeOmschrijving)
            .Ignore(dest => dest.InformatieObjectTypeOmschrijving)
            .AfterMapping(
                (src, dest) =>
                {
                    dest.BesluitTypeOmschrijving = Distinct(src.BesluitTypes, b => b.Omschrijving);
                    dest.InformatieObjectTypeOmschrijving = Distinct(src.InformatieObjectTypes, i => i.Omschrijving);
                }
            );

        config
            .NewConfig<ZaakType, V4.ZaakTypeResponseDto>()
            .Inherits<ZaakType, V3.ZaakTypeResponseDto>()
            .Ignore(dest => dest.Expand)
            .Ignore(dest => dest.BesluitTypeOmschrijving)
            .Ignore(dest => dest.InformatieObjectTypeOmschrijving)
            .Ignore(dest => dest.DeelZaakTypeIdentificaties)
            .Ignore(dest => dest.GerelateerdeZaakTypen)
            .AfterMapping(
                (src, dest) =>
                {
                    dest.BesluitTypeOmschrijving = Distinct(
                        src.ZaakTypeBesluitTypen,
                        b => Resolve(b.BesluitType?.Omschrijving, b.BesluitTypeOmschrijving)
                    );
                    dest.InformatieObjectTypeOmschrijving = Distinct(
                        src.ZaakTypeInformatieObjectTypen,
                        i => Resolve(i.InformatieObjectType?.Omschrijving, i.InformatieObjectTypeOmschrijving)
                    );
                    dest.DeelZaakTypeIdentificaties = Distinct(
                        src.ZaakTypeDeelZaakTypen,
                        d => Resolve(d.DeelZaakType?.Identificatie, d.DeelZaakTypeIdentificatie)
                    );

                    // Built from the source with the same rule as the v1.3 mapping (only the entries with a resolved GerelateerdeZaakType), not from the
                    // v1.3 list: that way it does not depend on the order in which Mapster runs the inherited .AfterMapping.
                    var uriService = MapContext.Current.GetService<IEntityUriService>();
                    dest.GerelateerdeZaakTypen = (src.ZaakTypeGerelateerdeZaakTypen ?? [])
                        .Where(z => z?.GerelateerdeZaakType != null)
                        .Select(z => new V4.GerelateerdeZaaktypeResponseDto
                        {
                            AardRelatie = z.AardRelatie.ToString(),
                            Toelichting = z.Toelichting,
                            ZaakType = uriService.GetUri(z.GerelateerdeZaakType),
                            ZaaktypeIdentificatie = Resolve(z.GerelateerdeZaakType.Identificatie, z.GerelateerdeZaakTypeIdentificatie),
                        })
                        .ToList();
                }
            );

        config
            .NewConfig<BesluitType, V4.BesluitTypeResponseDto>()
            .Inherits<BesluitType, V3.BesluitTypeResponseDto>()
            .Ignore(dest => dest.Expand)
            .Ignore(dest => dest.InformatieObjectTypeOmschrijvingen)
            .Ignore(dest => dest.ZaakTypeIdentificaties)
            .AfterMapping(
                (src, dest) =>
                {
                    dest.InformatieObjectTypeOmschrijvingen = Distinct(
                        src.BesluitTypeInformatieObjectTypen,
                        i => Resolve(i.InformatieObjectType?.Omschrijving, i.InformatieObjectTypeOmschrijving)
                    );
                    dest.ZaakTypeIdentificaties = Distinct(
                        src.BesluitTypeZaakTypen,
                        z => Resolve(z.ZaakType?.Identificatie, z.ZaakTypeIdentificatie)
                    );
                }
            );

        config
            .NewConfig<InformatieObjectType, V4.InformatieObjectTypeResponseDto>()
            .Inherits<InformatieObjectType, V3.InformatieObjectTypeResponseDto>()
            .Ignore(dest => dest.Expand)
            .Ignore(dest => dest.BesluitTypeOmschrijving)
            .Ignore(dest => dest.ZaakTypeIdentificaties)
            .AfterMapping(
                (src, dest) =>
                {
                    // Same filter as the besluittypen / zaaktypen URL lists of the v1.3 mapping, so the lists line up.
                    dest.BesluitTypeOmschrijving = Distinct(src.InformatieObjectTypeBesluitTypen, b => b.BesluitType?.Omschrijving);
                    dest.ZaakTypeIdentificaties = Distinct(src.InformatieObjectTypeZaakTypen, z => z.ZaakType?.Identificatie);
                }
            );

        config.NewConfig<Eigenschap, V4.EigenschapResponseDto>().Inherits<Eigenschap, V3.EigenschapResponseDto>().Ignore(dest => dest.Expand);

        config
            .NewConfig<ResultaatType, V4.ResultaatTypeResponseDto>()
            .Inherits<ResultaatType, V3.ResultaatTypeResponseDto>()
            .Ignore(dest => dest.Expand);

        config.NewConfig<RolType, V4.RolTypeResponseDto>().Inherits<RolType, V3.RolTypeResponseDto>().Ignore(dest => dest.Expand);

        config.NewConfig<StatusType, V4.StatusTypeResponseDto>().Inherits<StatusType, V3.StatusTypeResponseDto>().Ignore(dest => dest.Expand);

        // ResultaatTypeOmschrijving stays an empty list, like the (also deprecated) resultaattypen and statustypen of the v1.3 mapping: the relation
        // cannot be recorded yet (see the TODO there).
        config
            .NewConfig<ZaakObjectType, V4.ZaakObjectTypeResponseDto>()
            .Inherits<ZaakObjectType, V3.ZaakObjectTypeResponseDto>()
            .Ignore(dest => dest.Expand)
            .Ignore(dest => dest.ResultaatTypeOmschrijving);

        config
            .NewConfig<ZaakTypeInformatieObjectType, V4.ZaakTypeInformatieObjectTypeResponseDto>()
            .Inherits<ZaakTypeInformatieObjectType, V3.ZaakTypeInformatieObjectTypeResponseDto>()
            .Ignore(dest => dest.Expand);
    }

    // The lists below are distinct (the response schema says uniqueItems), materialized (see the remarks of the v1.3 mapping) and never null. A null
    // source list (navigation not loaded) gives an empty list; null join entries and entries without a value are left out.
    private static List<string> Distinct(IEnumerable<string> values) => values?.Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList() ?? [];

    private static List<string> Distinct<T>(IEnumerable<T> items, Func<T, string> select)
        where T : class => Distinct(items?.Where(i => i != null).Select(select));

    // The resolved (navigation) value first: the list handlers build the join entities in memory with the navigation only, so the stored soft
    // value is empty there. It is the fallback for a join entity whose target is not resolved.
    private static string Resolve(string resolved, string stored) => string.IsNullOrEmpty(resolved) ? stored : resolved;
}
