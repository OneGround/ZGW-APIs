using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.Common.Web.Expands.Fields;
using Xunit;
using V3 = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using V4 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The v1.4 ZaakType response hides the v1.3 <c>gerelateerdeZaaktypen</c> member (<c>new</c>) to give its elements the v1.4 type. The field selection
/// reflects over the public properties, which returns both members: it has to pick the v1.4 one.
/// </summary>
public class HiddenMemberFieldProjectionTests
{
    [Fact]
    public void Fields_selection_of_gerelateerdeZaaktypen_projects_the_v1_4_member()
    {
        var dto = new V4.ZaakTypeResponseDto
        {
            GerelateerdeZaakTypen = [new V4.GerelateerdeZaaktypeResponseDto { ZaakType = "u", ZaaktypeIdentificatie = "REL-1" }],
        };
        // What the inherited v1.3 mapping leaves in the hidden member
        ((V3.ZaakTypeResponseDto)dto).GerelateerdeZaakTypen = [new Catalogi.Contracts.v1.GerelateerdeZaaktypeDto { ZaakType = "u" }];

        var projected = FieldProjector.Project(dto, new FieldSelection { ScalarFields = ["gerelateerdeZaaktypen"] });

        var element = Assert.Single(
            Assert.IsAssignableFrom<IEnumerable<Catalogi.Contracts.v1.GerelateerdeZaaktypeDto>>(projected["gerelateerdeZaaktypen"])
        );
        Assert.Equal("REL-1", Assert.IsType<V4.GerelateerdeZaaktypeResponseDto>(element).ZaaktypeIdentificatie);
    }
}
