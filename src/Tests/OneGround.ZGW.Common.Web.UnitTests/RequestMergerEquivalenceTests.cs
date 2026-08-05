using System;
using AutoMapper;
using Mapster;
using MapsterMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests;

public class RequestMergerEquivalenceTests
{
    private sealed class Thing : IBaseEntity
    {
        public Guid Id { get; set; }
        public string Naam { get; set; }
        public string Toelichting { get; set; }
    }

    private sealed class ThingRequestDto
    {
        public string Naam { get; set; }
        public string Toelichting { get; set; }
    }

    private readonly JsonSerializer _serializer = new ZGWJsonSerializer();
    private readonly RequestMerger _autoMapperMerger;
    private readonly ZgwRequestMerger _mapsterMerger;

    public RequestMergerEquivalenceTests()
    {
        var autoMapperConfiguration = new MapperConfiguration(c => c.CreateMap<Thing, ThingRequestDto>());
        _autoMapperMerger = new RequestMerger(autoMapperConfiguration.CreateMapper());

        var mapsterConfig = new TypeAdapterConfig();
        mapsterConfig.NewConfig<Thing, ThingRequestDto>();
        mapsterConfig.Compile();
        _mapsterMerger = new ZgwRequestMerger(new MapsterMapper.Mapper(mapsterConfig));
    }

    // One instance per call, for clarity — PartialUpdateMerger.Merge never mutates the JObject it's given.
    private JObject Patch() => JObject.FromObject(new { naam = "gewijzigd" }, _serializer);

    private static string Serialize(object value) => JsonConvert.SerializeObject(value, new ZGWJsonSerializerSettings());

    [Fact]
    public void Both_mergers_produce_identical_output_for_the_same_patch()
    {
        var entity = new Thing
        {
            Id = Guid.NewGuid(),
            Naam = "origineel",
            Toelichting = "blijft staan",
        };

        var viaAutoMapper = _autoMapperMerger.MergePartialUpdateToObjectRequest<ThingRequestDto, Thing>(entity, Patch());
        var viaMapster = _mapsterMerger.MergePartialUpdateToObjectRequest<ThingRequestDto, Thing>(entity, Patch());

        Assert.Equal(Serialize(viaAutoMapper), Serialize(viaMapster));

        // Without these, two identically-broken results (e.g. both all-null) would satisfy the comparison
        // above. The patched field must come from the JObject and the untouched field from the entity.
        Assert.Equal("gewijzigd", viaMapster.Naam);
        Assert.Equal("blijft staan", viaMapster.Toelichting);
    }

    [Fact]
    public void Mapster_merger_rejects_a_non_JObject_payload_like_the_AutoMapper_one()
    {
        var entity = new Thing { Id = Guid.NewGuid(), Naam = "origineel" };

        Assert.Throws<InvalidOperationException>(() =>
            _mapsterMerger.MergePartialUpdateToObjectRequest<ThingRequestDto, Thing>(entity, "geen JObject")
        );
    }
}
