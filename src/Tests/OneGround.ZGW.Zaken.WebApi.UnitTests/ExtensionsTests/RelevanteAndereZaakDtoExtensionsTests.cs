using System.Collections.Generic;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Web.Extensions;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExtensionsTests;

public class RelevanteAndereZaakDtoExtensionsTests
{
    [Fact]
    public void GetUrls_returns_the_urls_in_order_as_sent()
    {
        List<RelevanteAndereZaakDto> relevanteAndereZaken =
        [
            new() { Url = "https://zrc.test/api/v1/zaken/1", AardRelatie = "vervolg" },
            new() { Url = "https://zrc.test/api/v1/zaken/2", AardRelatie = "bijdrage" },
            // The same zaak can be referenced more than once: both entries are validated
            new() { Url = "https://zrc.test/api/v1/zaken/1", AardRelatie = "onderwerp" },
        ];

        var urls = relevanteAndereZaken.GetUrls();

        Assert.Equal(["https://zrc.test/api/v1/zaken/1", "https://zrc.test/api/v1/zaken/2", "https://zrc.test/api/v1/zaken/1"], urls);
    }

    [Fact]
    public void GetUrls_of_an_empty_list_is_empty() => Assert.Empty(new List<RelevanteAndereZaakDto>().GetUrls());

    [Fact]
    public void GetUrls_of_a_missing_list_is_empty() => Assert.Empty(((List<RelevanteAndereZaakDto>)null).GetUrls());
}
