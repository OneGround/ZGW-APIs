using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

public class SearchRequestUuidFilterWireNameTests
{
    private const string Uuid = "11111111-1111-1111-1111-111111111111";
    private const string Body = $$"""{"uuid__in":["{{Uuid}}"]}""";

    [Fact]
    public void V1_5_SearchRequest_reads_uuid__in_from_the_body()
    {
        var dto = JsonConvert.DeserializeObject<Contracts.v1._5.Requests.EnkelvoudigInformatieObjectSearchRequestDto>(Body);

        var json = JObject.Parse(JsonConvert.SerializeObject(dto));
        Assert.Equal([Uuid], json["uuid__in"]?.ToObject<string[]>());
        Assert.False(json.ContainsKey("uuid_In"));
    }

    [Fact]
    public void V1_7_SearchRequest_reads_uuid__in_from_the_body()
    {
        var dto = JsonConvert.DeserializeObject<Contracts.v1._7.Requests.EnkelvoudigInformatieObjectSearchRequestDto>(Body);

        var json = JObject.Parse(JsonConvert.SerializeObject(dto));
        Assert.Equal([Uuid], json["uuid__in"]?.ToObject<string[]>());
        Assert.False(json.ContainsKey("uuid_In"));
    }
}
