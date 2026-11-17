using System;
using System.Linq;
using MapsterMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.DataModel;
using Xunit;
using V13 = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using V14 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests.v1_4;

/// <summary>
/// The read-only, denormalized lists that v1.4 adds to the responses (omschrijvingen and identificaties of related types). v1.3 must not get them.
/// </summary>
public class DomainToResponseV14AdditionsTests : IDisposable
{
    private readonly ZtcMapperTestHost _host = new();
    private IMapper Mapper => _host.Mapper;

    public void Dispose() => _host.Dispose();

    private static JObject Serialized(object value) => JObject.Parse(JsonConvert.SerializeObject(value));

    [Fact]
    public void Catalogus_lists_omschrijvingen_distinct()
    {
        var source = new Catalogus
        {
            Id = Guid.NewGuid(),
            ZaakTypes = [],
            BesluitTypes =
            [
                new BesluitType { Id = Guid.NewGuid(), Omschrijving = "B1" },
                new BesluitType { Id = Guid.NewGuid(), Omschrijving = "B1" },
            ],
            InformatieObjectTypes = [new InformatieObjectType { Id = Guid.NewGuid(), Omschrijving = "I1" }],
        };

        var result = Mapper.Map<V14.CatalogusResponseDto>(source);

        Assert.Equal(["B1"], result.BesluitTypeOmschrijving);
        Assert.Equal(["I1"], result.InformatieObjectTypeOmschrijving);
    }

    [Fact]
    public void Catalogus_without_loaded_navigations_gives_empty_lists()
    {
        var result = Mapper.Map<V14.CatalogusResponseDto>(new Catalogus { Id = Guid.NewGuid() });

        Assert.Empty(result.BesluitTypeOmschrijving);
        Assert.Empty(result.InformatieObjectTypeOmschrijving);
    }

    [Fact]
    public void ZaakType_lists_omschrijvingen_and_deelzaaktype_identificaties()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            StatusTypen = [],
            RolTypen = [],
            ResultaatTypen = [],
            Eigenschappen = [],
            ZaakObjectTypen = [],
            ZaakTypeGerelateerdeZaakTypen = [],
            ZaakTypeBesluitTypen =
            [
                new ZaakTypeBesluitType { BesluitTypeOmschrijving = "B1" },
                new ZaakTypeBesluitType { BesluitTypeOmschrijving = "B1" },
            ],
            ZaakTypeInformatieObjectTypen = [new ZaakTypeInformatieObjectType { InformatieObjectTypeOmschrijving = "I1" }],
            ZaakTypeDeelZaakTypen = [new ZaakTypeDeelZaakType { DeelZaakTypeIdentificatie = "D1" }],
        };

        var result = Mapper.Map<V14.ZaakTypeResponseDto>(source);

        Assert.Equal(["B1"], result.BesluitTypeOmschrijving);
        Assert.Equal(["I1"], result.InformatieObjectTypeOmschrijving);
        Assert.Equal(["D1"], result.DeelZaakTypeIdentificaties);
    }

    [Fact]
    public void ZaakType_without_loaded_navigations_gives_empty_lists()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            StatusTypen = [],
            RolTypen = [],
            ResultaatTypen = [],
            Eigenschappen = [],
            ZaakObjectTypen = [],
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var result = Mapper.Map<V14.ZaakTypeResponseDto>(source);

        Assert.Empty(result.BesluitTypeOmschrijving);
        Assert.Empty(result.InformatieObjectTypeOmschrijving);
        Assert.Empty(result.DeelZaakTypeIdentificaties);
    }

    [Fact]
    public void ZaakType_gerelateerde_zaaktypen_get_the_identificatie_on_the_right_element_and_the_serialized_element_otherwise_equals_v1_3()
    {
        var related = new ZaakType { Id = Guid.NewGuid() };
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            StatusTypen = [],
            RolTypen = [],
            ResultaatTypen = [],
            Eigenschappen = [],
            ZaakObjectTypen = [],
            ZaakTypeGerelateerdeZaakTypen =
            [
                // Not resolved: skipped by the v1.3 mapping, so it must not shift the identificaties of the entries after it.
                new ZaakTypeGerelateerdeZaakType { GerelateerdeZaakTypeIdentificatie = "UNRESOLVED", GerelateerdeZaakType = null },
                new ZaakTypeGerelateerdeZaakType
                {
                    GerelateerdeZaakTypeIdentificatie = "REL-1",
                    GerelateerdeZaakType = related,
                    Toelichting = "t",
                },
            ],
        };

        var v14 = Mapper.Map<V14.ZaakTypeResponseDto>(source);
        var v13 = Mapper.Map<V13.ZaakTypeResponseDto>(source);

        var element = Assert.Single(v14.GerelateerdeZaakTypen);
        Assert.Equal("REL-1", element.ZaaktypeIdentificatie);
        Assert.Equal(ZtcMapperTestHost.Resolved(related), element.ZaakType);

        // Serialized: exactly one "gerelateerdeZaaktypen" (the v1.4 member hides the v1.3 one), with the new member, and otherwise the v1.3 element
        var json14 = Serialized(v14);
        var json13 = Serialized(v13);
        var element14 = (JObject)json14["gerelateerdeZaaktypen"][0];
        var element13 = (JObject)json13["gerelateerdeZaaktypen"][0];
        Assert.Single(json14["gerelateerdeZaaktypen"]);
        Assert.Equal("REL-1", (string)element14["zaaktypeIdentificatie"]);
        Assert.False(element13.ContainsKey("zaaktypeIdentificatie"));
        element14.Remove("zaaktypeIdentificatie");
        Assert.Equal(element13.ToString(), element14.ToString());
    }

    [Fact]
    public void BesluitType_lists_informatieobjecttype_omschrijvingen_and_zaaktype_identificaties()
    {
        var source = new BesluitType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            BesluitTypeInformatieObjectTypen =
            [
                new BesluitTypeInformatieObjectType { InformatieObjectTypeOmschrijving = "I1" },
                new BesluitTypeInformatieObjectType { InformatieObjectTypeOmschrijving = "I1" },
            ],
            BesluitTypeZaakTypen = [new BesluitTypeZaakType { ZaakTypeIdentificatie = "Z1" }],
        };

        var result = Mapper.Map<V14.BesluitTypeResponseDto>(source);

        Assert.Equal(["I1"], result.InformatieObjectTypeOmschrijvingen);
        Assert.Equal(["Z1"], result.ZaakTypeIdentificaties);
    }

    // The list handlers build the join entities in memory with the navigation only (the denormalized string column stays empty), so the lists must
    // come from the navigation first. The tests above cover the column fallback (a join row whose target is not resolved).
    [Fact]
    public void BesluitType_lists_zaaktype_identificaties_from_the_navigation_when_the_column_is_empty()
    {
        var source = new BesluitType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            BesluitTypeInformatieObjectTypen =
            [
                new BesluitTypeInformatieObjectType { InformatieObjectType = new InformatieObjectType { Omschrijving = "I1" } },
            ],
            BesluitTypeZaakTypen =
            [
                new BesluitTypeZaakType
                {
                    ZaakType = new ZaakType { Id = Guid.NewGuid(), Identificatie = "Z1" },
                },
                new BesluitTypeZaakType
                {
                    ZaakType = new ZaakType { Id = Guid.NewGuid(), Identificatie = "Z2" },
                },
            ],
        };

        var result = Mapper.Map<V14.BesluitTypeResponseDto>(source);

        Assert.Equal(["Z1", "Z2"], result.ZaakTypeIdentificaties);
        Assert.Equal(["I1"], result.InformatieObjectTypeOmschrijvingen);
        Assert.Equal(2, result.ZaakTypen.Count());
    }

    [Fact]
    public void ZaakType_lists_come_from_the_navigation_when_the_columns_are_empty()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            StatusTypen = [],
            RolTypen = [],
            ResultaatTypen = [],
            Eigenschappen = [],
            ZaakObjectTypen = [],
            ZaakTypeGerelateerdeZaakTypen = [],
            ZaakTypeBesluitTypen =
            [
                new ZaakTypeBesluitType
                {
                    BesluitType = new BesluitType { Id = Guid.NewGuid(), Omschrijving = "B1" },
                },
            ],
            ZaakTypeInformatieObjectTypen =
            [
                new ZaakTypeInformatieObjectType { InformatieObjectType = new InformatieObjectType { Omschrijving = "I1" } },
            ],
            ZaakTypeDeelZaakTypen =
            [
                new ZaakTypeDeelZaakType
                {
                    DeelZaakType = new ZaakType { Id = Guid.NewGuid(), Identificatie = "D1" },
                },
            ],
        };

        var result = Mapper.Map<V14.ZaakTypeResponseDto>(source);

        Assert.Equal(["B1"], result.BesluitTypeOmschrijving);
        Assert.Equal(["I1"], result.InformatieObjectTypeOmschrijving);
        Assert.Equal(["D1"], result.DeelZaakTypeIdentificaties);
    }

    [Fact]
    public void BesluitType_without_loaded_navigations_gives_empty_lists()
    {
        var result = Mapper.Map<V14.BesluitTypeResponseDto>(
            new BesluitType
            {
                Id = Guid.NewGuid(),
                Catalogus = new Catalogus { Id = Guid.NewGuid() },
            }
        );

        Assert.Empty(result.InformatieObjectTypeOmschrijvingen);
        Assert.Empty(result.ZaakTypeIdentificaties);
    }

    [Fact]
    public void InformatieObjectType_lists_besluittype_omschrijvingen_and_zaaktype_identificaties_of_resolved_entries_only()
    {
        var source = new InformatieObjectType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            InformatieObjectTypeBesluitTypen =
            [
                new BesluitTypeInformatieObjectType
                {
                    BesluitType = new BesluitType { Id = Guid.NewGuid(), Omschrijving = "B1" },
                },
                new BesluitTypeInformatieObjectType { BesluitType = null },
            ],
            InformatieObjectTypeZaakTypen =
            [
                new ZaakTypeInformatieObjectType
                {
                    ZaakType = new ZaakType { Id = Guid.NewGuid(), Identificatie = "Z1" },
                },
                new ZaakTypeInformatieObjectType { ZaakType = null },
            ],
        };

        var result = Mapper.Map<V14.InformatieObjectTypeResponseDto>(source);

        Assert.Equal(["B1"], result.BesluitTypeOmschrijving);
        Assert.Equal(["Z1"], result.ZaakTypeIdentificaties);
    }

    [Fact]
    public void ZaakObjectType_resultaattype_omschrijving_is_an_empty_list()
    {
        var source = new ZaakObjectType
        {
            Id = Guid.NewGuid(),
            ZaakType = new ZaakType
            {
                Id = Guid.NewGuid(),
                Catalogus = new Catalogus { Id = Guid.NewGuid() },
            },
        };

        var result = Mapper.Map<V14.ZaakObjectTypeResponseDto>(source);

        Assert.NotNull(result.ResultaatTypeOmschrijving);
        Assert.Empty(result.ResultaatTypeOmschrijving);
    }

    [Fact]
    public void V1_3_responses_do_not_contain_the_v1_4_additions()
    {
        var catalogus = Serialized(Mapper.Map<V13.CatalogusResponseDto>(new Catalogus { Id = Guid.NewGuid() }));
        var besluitType = Serialized(
            Mapper.Map<V13.BesluitTypeResponseDto>(
                new BesluitType
                {
                    Id = Guid.NewGuid(),
                    Catalogus = new Catalogus { Id = Guid.NewGuid() },
                }
            )
        );
        var zaakObjectType = Serialized(
            Mapper.Map<V13.ZaakObjectTypeResponseDto>(
                new ZaakObjectType
                {
                    Id = Guid.NewGuid(),
                    ZaakType = new ZaakType
                    {
                        Id = Guid.NewGuid(),
                        Catalogus = new Catalogus { Id = Guid.NewGuid() },
                    },
                }
            )
        );

        Assert.False(catalogus.ContainsKey("besluittypeOmschrijving"));
        Assert.False(catalogus.ContainsKey("informatieobjecttypeOmschrijving"));
        Assert.False(besluitType.ContainsKey("zaaktypeIdentificaties"));
        Assert.False(besluitType.ContainsKey("informatieobjecttypeOmschrijvingen"));
        Assert.False(zaakObjectType.ContainsKey("resultaattypeOmschrijving"));
    }

    [Fact]
    public void V1_4_additions_serialize_under_the_spec_names()
    {
        var json = Serialized(Mapper.Map<V14.CatalogusResponseDto>(new Catalogus { Id = Guid.NewGuid() }));

        Assert.True(json.ContainsKey("besluittypeOmschrijving"));
        Assert.True(json.ContainsKey("informatieobjecttypeOmschrijving"));
    }

    [Fact]
    public void V1_4_additions_follow_the_property_they_belong_to()
    {
        static void AssertFollows(JObject json, string first, string second)
        {
            var names = json.Properties().Select(p => p.Name).ToList();
            Assert.True(
                names.IndexOf(first) >= 0 && names.IndexOf(second) == names.IndexOf(first) + 1,
                $"'{second}' must directly follow '{first}': {string.Join(",", names)}"
            );
        }

        var catalogus = Serialized(Mapper.Map<V14.CatalogusResponseDto>(new Catalogus { Id = Guid.NewGuid() }));
        AssertFollows(catalogus, "besluittypen", "besluittypeOmschrijving");
        AssertFollows(catalogus, "informatieobjecttypen", "informatieobjecttypeOmschrijving");

        var besluitType = Serialized(
            Mapper.Map<V14.BesluitTypeResponseDto>(
                new BesluitType
                {
                    Id = Guid.NewGuid(),
                    Catalogus = new Catalogus { Id = Guid.NewGuid() },
                }
            )
        );
        AssertFollows(besluitType, "zaaktypen", "zaaktypeIdentificaties");
        AssertFollows(besluitType, "informatieobjecttypen", "informatieobjecttypeOmschrijvingen");
        AssertFollows(besluitType, "resultaattypen", "resultaattypenOmschrijving");

        var iot = Serialized(
            Mapper.Map<V14.InformatieObjectTypeResponseDto>(
                new InformatieObjectType
                {
                    Id = Guid.NewGuid(),
                    Catalogus = new Catalogus { Id = Guid.NewGuid() },
                }
            )
        );
        AssertFollows(iot, "zaaktypen", "zaaktypeIdentificaties");
        AssertFollows(iot, "besluittypen", "besluittypeOmschrijving");

        var zaakType = Serialized(
            Mapper.Map<V14.ZaakTypeResponseDto>(
                new ZaakType
                {
                    Id = Guid.NewGuid(),
                    Catalogus = new Catalogus { Id = Guid.NewGuid() },
                    StatusTypen = [],
                    RolTypen = [],
                    ResultaatTypen = [],
                    Eigenschappen = [],
                    ZaakObjectTypen = [],
                    ZaakTypeGerelateerdeZaakTypen = [],
                }
            )
        );
        AssertFollows(zaakType, "besluittypen", "besluittypeOmschrijving");
        AssertFollows(zaakType, "deelzaaktypen", "deelzaaktypeIdentificaties");
        AssertFollows(zaakType, "informatieobjecttypen", "informatieobjecttypeOmschrijving");

        var zaakObjectType = Serialized(
            Mapper.Map<V14.ZaakObjectTypeResponseDto>(
                new ZaakObjectType
                {
                    Id = Guid.NewGuid(),
                    ZaakType = new ZaakType
                    {
                        Id = Guid.NewGuid(),
                        Catalogus = new Catalogus { Id = Guid.NewGuid() },
                    },
                }
            )
        );
        AssertFollows(zaakObjectType, "resultaattypen", "resultaattypeOmschrijving");
    }
}
