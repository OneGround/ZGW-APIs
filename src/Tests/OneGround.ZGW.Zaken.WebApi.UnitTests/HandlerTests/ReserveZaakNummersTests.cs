using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Controllers.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._7;
using OneGround.ZGW.Zaken.Web.Validators.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests;

// Note: reserving a zaaknummer = generating a number that is never handed out again. The caller can only reserve for its own organisation.
public class ReserveZaakNummersTests : IAsyncLifetime
{
    private const string TestOwner = "999993653";

    private ZrcDbContext _dbContext;
    private Mock<INummerGenerator> _nummerGeneratorMock;
    private Mock<IAuthorizationContextAccessor> _authorizationContextAccessorMock;
    private int _counter;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ZrcDbContext>().UseInMemoryDatabase(databaseName: $"zrc-{Guid.NewGuid()}").Options;

        _dbContext = new UnitTestZrcDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _nummerGeneratorMock = new Mock<INummerGenerator>();
        _nummerGeneratorMock
            .Setup(m => m.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"Z2025-{++_counter:D8}");

        _authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        _authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    private ReserveZaakNummersCommandHandler CreateSut(string zakenFormat = "Z{yyyy}-{v^8}", string maxAantal = null)
    {
        _nummerGeneratorMock.Setup(m => m.GetFormatAsync(TestOwner, "zaken", It.IsAny<CancellationToken>())).ReturnsAsync(zakenFormat);

        var settings = new Dictionary<string, string>();
        if (maxAantal != null)
        {
            settings["Application:ReserveZaaknummersMaxAantal"] = maxAantal;
        }

        return new ReserveZaakNummersCommandHandler(
            NullLogger<ReserveZaakNummersCommandHandler>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            _authorizationContextAccessorMock.Object,
            _dbContext,
            _nummerGeneratorMock.Object
        );
    }

    // Note: no number may be used up when the request is rejected
    private void AssertNoNumberGenerated() =>
        _nummerGeneratorMock.Verify(
            m => m.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, bool>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

    private static ReserveZaakNummersCommand Command(string bronorganisatie = TestOwner, int aantal = 1) =>
        new() { Bronorganisatie = bronorganisatie, Aantal = aantal };

    // ---- handler

    [Fact]
    public async Task Reserve_ReturnsTheRequestedNumberOfUniqueNumbers_ForTheOrganisationOfTheCaller()
    {
        var result = await CreateSut().Handle(Command(aantal: 3), CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Equal(["Z2025-00000001", "Z2025-00000002", "Z2025-00000003"], result.Result);
        _nummerGeneratorMock.Verify(
            m => m.GenerateAsync(TestOwner, "zaken", It.IsAny<Func<string, bool>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3)
        );
    }

    [Fact]
    public async Task Reserve_ForAnotherBronorganisatie_IsAValidationError_AndUsesNoNumber()
    {
        var result = await CreateSut().Handle(Command(bronorganisatie: "123456782"), CancellationToken.None);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        Assert.Equal("bronorganisatie", result.Errors.Single().Name);
        AssertNoNumberGenerated();
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public async Task Reserve_IsLimitedTo100_ByDefault(int aantal, bool expectOk)
    {
        var result = await CreateSut().Handle(Command(aantal: aantal), CancellationToken.None);

        Assert.Equal(expectOk ? CommandStatus.OK : CommandStatus.ValidationError, result.Status);
        if (!expectOk)
        {
            Assert.Equal("aantal", result.Errors.Single().Name);
            AssertNoNumberGenerated();
        }
    }

    [Fact]
    public async Task Reserve_TheLimitCanBeConfigured()
    {
        var result = await CreateSut(maxAantal: "5").Handle(Command(aantal: 6), CancellationToken.None);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
    }

    [Fact]
    public async Task Reserve_WithAFormatThatNeedsTheZaaktype_IsAValidationError_AndUsesNoNumber()
    {
        var result = await CreateSut(zakenFormat: "{ztc}-{v^8}").Handle(Command(), CancellationToken.None);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        AssertNoNumberGenerated();
    }

    [Fact]
    public async Task Reserve_ChecksUniquenessAgainstTheZakenOfTheOrganisation()
    {
        _dbContext.Zaken.Add(
            new Zaak
            {
                Id = Guid.NewGuid(),
                Owner = TestOwner,
                Zaaktype = "http://catalogi.local/api/v1/zaaktypen/33333333-3333-3333-3333-333333333333",
                Bronorganisatie = TestOwner,
                Identificatie = "BESTAAT",
                Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
                VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
                Archiefstatus = ArchiefStatus.nog_te_archiveren,
                Communicatiekanaal = string.Empty,
                Selectielijstklasse = string.Empty,
                VerantwoordelijkeOrganisatie = TestOwner,
            }
        );
        await _dbContext.SaveChangesAsync();

        Func<string, bool> isUnique = null;
        _nummerGeneratorMock
            .Setup(m => m.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, bool>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<string, bool>, CancellationToken>((_, _, unique, _) => isUnique = unique)
            .ReturnsAsync("NIEUW");

        await CreateSut().Handle(Command(), CancellationToken.None);

        Assert.False(isUnique("BESTAAT"));
        Assert.True(isUnique("NIEUW"));
    }

    // ---- validator

    [Theory]
    [InlineData(null, true)] // Note: required
    [InlineData("12345", true)]
    [InlineData("123456789", true)] // Note: fails the 11-proef
    [InlineData(TestOwner, false)]
    public void Validator_Bronorganisatie(string bronorganisatie, bool expectError)
    {
        var result = new ReserveZaakIdentificatieRequestValidator().TestValidate(
            new ReserveZaakIdentificatieRequestDto { Bronorganisatie = bronorganisatie }
        );

        Assert.Equal(expectError, result.Errors.Any(e => e.PropertyName.ToLower() == "bronorganisatie"));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(1, false)]
    [InlineData(100, false)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    public void Validator_Aantal(int? aantal, bool expectError)
    {
        var result = new ReserveZaakIdentificatieRequestValidator().TestValidate(
            new ReserveZaakIdentificatieRequestDto { Bronorganisatie = TestOwner, Aantal = aantal }
        );

        Assert.Equal(expectError, result.Errors.Any(e => e.PropertyName.ToLower() == "aantal"));
    }

    // ---- controller: the shape of the response

    private async Task<ObjectResult> ReserveAsync(int? aantal, params string[] nummers)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<CommandResult<IReadOnlyList<string>>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult<IReadOnlyList<string>>(nummers, CommandStatus.OK));

        var controller = new ZaaknummerReserverenController(
            NullLogger<ZaaknummerReserverenController>.Instance,
            mediatorMock.Object,
            new Mock<MapsterMapper.IMapper>().Object,
            new Mock<IErrorResponseBuilder>().Object
        );

        var result = await controller.ReserveAsync(new ReserveZaakIdentificatieRequestDto { Bronorganisatie = TestOwner, Aantal = aantal });

        return Assert.IsType<ObjectResult>(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task Controller_OneReservation_ReturnsASingleObject(int? aantal)
    {
        var result = await ReserveAsync(aantal, "Z2025-00000001");

        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal("Z2025-00000001", Assert.IsType<ReserveZaakIdentificatieResponseDto>(result.Value).Zaaknummer);
    }

    [Fact]
    public async Task Controller_MoreReservations_ReturnsAList()
    {
        var result = await ReserveAsync(2, "Z2025-00000001", "Z2025-00000002");

        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        var list = Assert.IsType<List<ReserveZaakIdentificatieResponseDto>>(result.Value);
        Assert.Equal(["Z2025-00000001", "Z2025-00000002"], list.Select(r => r.Zaaknummer));
    }
}
