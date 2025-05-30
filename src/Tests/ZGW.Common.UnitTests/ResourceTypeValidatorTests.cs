using Roxit.ZGW.Common.Services;
using Xunit;

namespace Roxit.ZGW.Common.UnitTests;

public class ResourceTypeValidatorTests
{
    private const string ZaakTypes = "zaaktypen";
    private const string Catalogs = "catalogussen";

    [Fact]
    public void Validator_With_Bad_Guid_Should_Give_Error()
    {
        var url = "https://catalogi-api.vng.cloud/api/v1/zaaktypen/398f1-d86c-4592-8ed7-ddc97419f80d";

        var result = ResourceTypeValidator.IsOfType(ZaakTypes, url);

        Assert.False(result);
    }

    [Fact]
    public void Validator_With_Mismatch_Resource_Should_Give_Error()
    {
        var url = "https://catalogi-api.vng.cloud/api/v1/zaaktypen/495398f1-d86c-4592-8ed7-ddc97419f80d";

        var result = ResourceTypeValidator.IsOfType(Catalogs, url);

        Assert.False(result);
    }

    [Fact]
    public void Validator_Happy_Flow_Should_Pass()
    {
        var url = "https://catalogi-api.vng.cloud/api/v1/zaaktypen/495398f1-d86c-4592-8ed7-ddc97419f80d";

        var result = ResourceTypeValidator.IsOfType(ZaakTypes, url);

        Assert.True(result);
    }
}
