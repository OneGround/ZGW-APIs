using System;
using MapsterMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Models.v1._5;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_7;

// Discriminates that MappingProfiles.v1._7.RequestToDomainRegister actually registers its own
// ZaakSearchRequestDto->GetAllZakenFilter config for the v1._7 concrete type, rather than silently
// falling through to Mapster's naive convention mapping (which would not parse date strings /
// convert "__in" string arrays to the destination's typed arrays the way the custom .Map calls do).
public class RequestToDomainProfileTests : IDisposable
{
    private const string TestRsin = "999993653";

    private readonly ZrcMapperTestHost _host = new();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void ZaakSearchRequestDto_v1_7_Maps_To_GetAllZakenFilter()
    {
        var source = new ZaakSearchRequestDto
        {
            Archiefactiedatum = "2020-11-05",
            Archiefnominatie__in = [ArchiefNominatie.vernietigen.ToString()],
            Archiefstatus__in = [ArchiefStatus.overgedragen.ToString()],
            Bronorganisatie__in = [TestRsin],
            Uuid__in = ["9337ba82-999a-4440-aa02-2b7b0b6c33f6"],
            Zaaktype__in = ["https://example.test/zaaktypen/1"],
        };

        var result = _mapper.Map<GetAllZakenFilter>(source);

        Assert.Equal(new DateOnly(2020, 11, 5), result.Archiefactiedatum);
        Assert.Equal([ArchiefNominatie.vernietigen], result.Archiefnominatie__in);
        Assert.Equal([ArchiefStatus.overgedragen], result.Archiefstatus__in);
        Assert.Equal([TestRsin], result.Bronorganisatie__in);
        Assert.Equal([new Guid("9337ba82-999a-4440-aa02-2b7b0b6c33f6")], result.Uuid__in);
        Assert.Equal(["https://example.test/zaaktypen/1"], result.Zaaktype__in);
    }

    [Fact]
    public void ZaakSearchRequestDto_v1_7_with_null_arrays_Maps_To_GetAllZakenFilter_with_empty_arrays()
    {
        var source = new ZaakSearchRequestDto
        {
            Archiefnominatie__in = null,
            Archiefstatus__in = null,
            Bronorganisatie__in = null,
            Uuid__in = null,
            Zaaktype__in = null,
        };

        var result = _mapper.Map<GetAllZakenFilter>(source);

        Assert.NotNull(result.Archiefnominatie__in);
        Assert.Empty(result.Archiefnominatie__in);
        Assert.NotNull(result.Uuid__in);
        Assert.Empty(result.Uuid__in);
    }
}
