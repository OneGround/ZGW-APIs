using System;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_2;

/// <summary>
/// Covers the v1.2-only PATCH merge for ZAAKOBJECTen, run through the real Mapster seam
/// (<see cref="ZrcMapperTestHost"/>) and a real <see cref="ZgwRequestMerger"/> — never a hand-rolled
/// <c>TypeAdapterConfig</c> and never a hand-copy of the merge logic, since either would drift from what
/// the controller actually does.
/// </summary>
public class RequestMergerTests : IDisposable
{
    private readonly ZrcMapperTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private static ZaakObject CreateZaakObjectWithOverigeDefinitie()
    {
        return new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = new Zaak { Id = Guid.NewGuid() },
            Object = "https://example.test/objecten/1",
            ObjectType = ObjectType.overige,
            ObjectTypeOverige = "some-custom-type",
            ObjectTypeOverigeDefinitie = new ObjectTypeOverigeDefinitie
            {
                Id = Guid.NewGuid(),
                Url = "https://example.test/schemas/some-custom-type",
                Schema = "some-schema",
                ObjectData = "{}",
            },
            RelatieOmschrijving = "oorspronkelijke omschrijving",
        };
    }

    [Fact]
    public void PartialUpdate_on_v1_2_ZaakObject_keeps_objectTypeOverigeDefinitie_when_the_patch_omits_it()
    {
        // afterMap must set Version = "1.2" on the mapped request BEFORE PartialUpdateMerger.Merge
        // serializes it: ZaakObjectDto.ShouldSerializeObjectTypeOverigeDefinitie() only returns true when
        // Version == "1.2", and that check runs during that serialization, not on the value this method
        // returns. Get the timing wrong and a v1.2 PATCH that never mentions objectTypeOverigeDefinitie
        // silently drops it.
        var merger = new ZgwRequestMerger(_host.Mapper);
        var existing = CreateZaakObjectWithOverigeDefinitie();
        var patch = new JObject { ["relatieomschrijving"] = "nieuwe omschrijving" };

        var merged = merger.MergePartialUpdateToObjectRequest<ZaakObjectRequestDto, ZaakObject>(existing, patch, request => request.Version = "1.2");

        Assert.Equal("nieuwe omschrijving", merged.RelatieOmschrijving); // the merge itself still applied
        Assert.NotNull(merged.ObjectTypeOverigeDefinitie);
        Assert.Equal(existing.ObjectTypeOverigeDefinitie.Url, merged.ObjectTypeOverigeDefinitie.Url);
        Assert.Equal(existing.ObjectTypeOverigeDefinitie.Schema, merged.ObjectTypeOverigeDefinitie.Schema);
        Assert.Equal(existing.ObjectTypeOverigeDefinitie.ObjectData, merged.ObjectTypeOverigeDefinitie.ObjectData);
    }
}
