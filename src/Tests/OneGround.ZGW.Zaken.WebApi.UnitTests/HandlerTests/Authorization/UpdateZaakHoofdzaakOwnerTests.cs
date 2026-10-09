using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using Xunit;
using static OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization.ZrcAuthorizationTestSupport;
using V1 = OneGround.ZGW.Zaken.Web.Handlers.v1;
using V1_5 = OneGround.ZGW.Zaken.Web.Handlers.v1._5;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization;

public class UpdateZaakHoofdzaakOwnerTests : IDisposable
{
    private const string HoofdzaakUrl = "http://zaken.local/api/v1/zaken/ffffffff-ffff-ffff-ffff-ffffffffffff";

    private readonly ZrcDbContext _context = CreateDbContext();
    private readonly Mock<IEntityUriService> _uriService = new();
    private readonly Mock<IZaakBusinessRuleService> _businessRules = new();
    private readonly Mock<IClosedZaakModificationBusinessRule> _closedZaakRule = new();

    public UpdateZaakHoofdzaakOwnerTests()
    {
        // Simulate validation passing, to prove the handler's own lookup is owner-filtered too.
        _businessRules
            .Setup(r => r.ValidateAsync(It.IsAny<Zaak>(), It.IsAny<Zaak>(), It.IsAny<string>(), It.IsAny<List<ValidationError>>()))
            .ReturnsAsync(true);
        _closedZaakRule.Setup(r => r.ValidateClosedZaakModificationRule(It.IsAny<Zaak>(), It.IsAny<List<ValidationError>>())).Returns(true);
    }

    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task Hoofdzaak_of_another_organisation_is_not_linked(string version)
    {
        var original = NewZaak(CurrentTenant, ZaakTypeA);
        var foreign = NewZaak(OtherTenant, ZaakTypeB);
        _context.Zaken.AddRange(original, foreign);
        await _context.SaveChangesAsync();
        _uriService.Setup(u => u.GetId(HoofdzaakUrl)).Returns(foreign.Id);

        var result = await UpdateAsync(version, original);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        Assert.Contains(result.Errors, e => e.Name == "hoofdzaak" && e.Code == ErrorCode.NoMatch);
        Assert.Null(_context.Zaken.AsNoTracking().Single(z => z.Id == original.Id).HoofdzaakId);
    }

    private Task<CommandResult<Zaak>> UpdateAsync(string version, Zaak original)
    {
        var client = ClientWithAllAuthorizations(AuthorizationScopes.Zaken.Update);

        return version switch
        {
            "v1" => new V1.UpdateZaakCommandHandler(
                NullLogger<V1.UpdateZaakCommandHandler>.Instance,
                CreateConfiguration(),
                _context,
                _uriService.Object,
                _businessRules.Object,
                Mock.Of<INotificatieService>(),
                Mock.Of<IEntityUpdater<Zaak>>(),
                AuditTrailFactory().Object,
                _closedZaakRule.Object,
                client,
                KenmerkenResolver().Object
            ).Handle(
                new V1.UpdateZaakCommand
                {
                    Id = original.Id,
                    OriginalZaak = original,
                    Zaak = new Zaak { Zaaktype = ZaakTypeA },
                    HoofdzaakUrl = HoofdzaakUrl,
                },
                CancellationToken.None
            ),
            "v1.5" => new V1_5.UpdateZaakCommandHandler(
                NullLogger<V1_5.UpdateZaakCommandHandler>.Instance,
                CreateConfiguration(),
                _context,
                _uriService.Object,
                _businessRules.Object,
                Mock.Of<INotificatieService>(),
                Mock.Of<IEntityUpdater<Zaak>>(),
                AuditTrailFactory().Object,
                _closedZaakRule.Object,
                client,
                KenmerkenResolver().Object
            ).Handle(
                new V1_5.UpdateZaakCommand
                {
                    Id = original.Id,
                    OriginalZaak = original,
                    Zaak = new Zaak { Zaaktype = ZaakTypeA },
                    HoofdzaakUrl = HoofdzaakUrl,
                    SRID = 28992,
                },
                CancellationToken.None
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }
}
