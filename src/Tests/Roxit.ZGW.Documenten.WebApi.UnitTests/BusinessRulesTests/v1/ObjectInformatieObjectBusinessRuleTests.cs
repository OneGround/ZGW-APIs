using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Roxit.ZGW.Besluiten.Contracts.v1.Queries;
using Roxit.ZGW.Besluiten.Contracts.v1.Responses;
using Roxit.ZGW.Besluiten.ServiceAgent.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.BusinessRules.v1;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.ServiceAgent.v1;
using Xunit;

namespace Roxit.ZGW.Documenten.WebApi.UnitTests.BusinessRulesTests.v1;

public class ObjectInformatieObjectBusinessRuleTests
{
    [Fact]
    public async Task Add_With_Existing_InformatieObject_And_Object_Should_Be_Invalid()
    {
        var mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext());

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();
        var mockBesluitenServiceAgent = new Mock<IBesluitenServiceAgent>();
        var mockEntityUriService = new Mock<IEntityUriService>();
        mockEntityUriService.Setup(s => s.GetId(It.IsAny<string>())).Returns(Guid.Parse("2793aeca-04e9-4b3d-8e63-2fb21a441158"));

        var svc = new ObjectInformatieObjectBusinessRuleService(
            mockDbContext,
            mockEntityUriService.Object,
            mockZakenServiceAgent.Object,
            mockBesluitenServiceAgent.Object
        );

        var objectInformatieObject = new ObjectInformatieObject { Object = "/zaken/03018e8e-f8ab-4295-8222-842cf450a2da" };

        var informatieObjectUrl = "/enkelvoudiginformatieobjecten/2793aeca-04e9-4b3d-8e63-2fb21a441158";

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(objectInformatieObject, informatieObjectUrl, ignoreZaakAndBesluitObjectValidation: true, errors);

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.InconsistentRelation);

        Assert.NotNull(error);
        Assert.Equal("nonFieldErrors", error.Name);
    }

    [Fact]
    public async Task Add_With_Unqiue_InformatieObject_And_Object_Should_Be_Valid()
    {
        var mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext());

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();
        var mockBesluitenServiceAgent = new Mock<IBesluitenServiceAgent>();
        var mockEntityUriService = new Mock<IEntityUriService>();

        var svc = new ObjectInformatieObjectBusinessRuleService(
            mockDbContext,
            mockEntityUriService.Object,
            mockZakenServiceAgent.Object,
            mockBesluitenServiceAgent.Object
        );

        var objectInformatieObject = new ObjectInformatieObject
        {
            Object = "http://zaken.user.local:5005/api/v1/zaken/03018e8e-f8ab-4295-8222-842cf450a2da",
        };

        var informatieObjectUrl = "http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/ff93aeca-24e2-4b3d-ae63-1fb21a4411c3";

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(objectInformatieObject, informatieObjectUrl, ignoreZaakAndBesluitObjectValidation: true, errors);

        Assert.True(valid);
    }

    [Fact]
    public async Task Add_With_Unknown_Zaak_Should_Be_Invalid()
    {
        var mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext());

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();
        var mockBesluitenServiceAgent = new Mock<IBesluitenServiceAgent>();
        var mockEntityUriService = new Mock<IEntityUriService>();

        mockZakenServiceAgent
            .Setup(m => m.GetZaakByUrlAsync("http://zaken.user.local:5005/api/v1/zaken/03018e8e-f8ab-4295-8222-842cf450a2da"))
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<ZaakResponseDto>(
                        new ErrorResponse
                        {
                            Status = (int)HttpStatusCode.NotFound,
                            Title = "Not found.",
                            Code = ErrorCode.BadUrl,
                        }
                    )
                )
            );

        var svc = new ObjectInformatieObjectBusinessRuleService(
            mockDbContext,
            mockEntityUriService.Object,
            mockZakenServiceAgent.Object,
            mockBesluitenServiceAgent.Object
        );

        var objectInformatieObject = new ObjectInformatieObject
        {
            Object = "http://zaken.user.local:5005/api/v1/zaken/03018e8e-f8ab-4295-8222-842cf450a2da",
            ObjectType = ObjectType.zaak,
        };

        var informatieObjectUrl = "http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/2793aeca-04e9-4b3d-8e63-2fb21a441158";

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(objectInformatieObject, informatieObjectUrl, ignoreZaakAndBesluitObjectValidation: false, errors);

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.BadUrl);

        Assert.NotNull(error);
        Assert.Equal("object", error.Name);
    }

    [Fact]
    public async Task Add_With_ServerError_Zaak_Should_Be_Invalid()
    {
        var mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext());

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();
        var mockBesluitenServiceAgent = new Mock<IBesluitenServiceAgent>();
        var mockEntityUriService = new Mock<IEntityUriService>();

        mockZakenServiceAgent
            .Setup(m => m.GetZaakByUrlAsync("http://zaken.user.local:5005/api/v1/zaken/03018e8e-f8ab-4295-8222-842cf450a2da"))
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<ZaakResponseDto>(
                        new ErrorResponse
                        {
                            Status = (int)HttpStatusCode.InternalServerError,
                            Title = "Server error.",
                            Code = ErrorCode.BadUrl,
                        }
                    )
                )
            );

        var svc = new ObjectInformatieObjectBusinessRuleService(
            mockDbContext,
            mockEntityUriService.Object,
            mockZakenServiceAgent.Object,
            mockBesluitenServiceAgent.Object
        );

        var objectInformatieObject = new ObjectInformatieObject
        {
            Object = "http://zaken.user.local:5005/api/v1/zaken/03018e8e-f8ab-4295-8222-842cf450a2da",
            ObjectType = ObjectType.zaak,
        };

        var informatieObjectUrl = "http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/2793aeca-04e9-4b3d-8e63-2fb21a441158";

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(objectInformatieObject, informatieObjectUrl, ignoreZaakAndBesluitObjectValidation: false, errors);

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.BadUrl);

        Assert.NotNull(error);
        Assert.Equal("object", error.Name);
    }

    [Fact]
    public async Task Add_With_Known_Zaak_Should_Be_Valid()
    {
        var mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext());

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();
        var mockBesluitenServiceAgent = new Mock<IBesluitenServiceAgent>();
        var mockEntityUriService = new Mock<IEntityUriService>();

        mockZakenServiceAgent
            .Setup(m => m.GetZaakByUrlAsync("http://zaken.user.local:5005/api/v1/zaken/ff93aeca-24e2-4b3d-ae63-1fb21a4411c3"))
            .Returns(Task.FromResult(new ServiceAgentResponse<ZaakResponseDto>(response: new ZaakResponseDto())));

        mockZakenServiceAgent
            .Setup(m =>
                m.GetZaakInformatieObjectenAsync(
                    It.Is<GetAllZaakInformatieObjectenQueryParameters>(s =>
                        s.Zaak == "http://zaken.user.local:5005/api/v1/zaken/ff93aeca-24e2-4b3d-ae63-1fb21a4411c3"
                        && s.InformatieObject
                            == "http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/2793aeca-04e9-4b3d-8e63-2fb21a441158"
                    )
                )
            )
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<IEnumerable<ZaakInformatieObjectResponseDto>>(
                        response:
                        [
                            new ZaakInformatieObjectResponseDto
                            {
                                Zaak = "http://zaken.user.local:5005/api/v1/zaken/ff93aeca-24e2-4b3d-ae63-1fb21a4411c3",
                            },
                        ]
                    )
                )
            );

        var svc = new ObjectInformatieObjectBusinessRuleService(
            mockDbContext,
            mockEntityUriService.Object,
            mockZakenServiceAgent.Object,
            mockBesluitenServiceAgent.Object
        );

        var objectInformatieObject = new ObjectInformatieObject
        {
            Object = "http://zaken.user.local:5005/api/v1/zaken/ff93aeca-24e2-4b3d-ae63-1fb21a4411c3",
            ObjectType = ObjectType.zaak,
        };

        var informatieObjectUrl = "http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/2793aeca-04e9-4b3d-8e63-2fb21a441158";

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(objectInformatieObject, informatieObjectUrl, ignoreZaakAndBesluitObjectValidation: false, errors);

        Assert.True(valid);
    }

    [Fact]
    public async Task Add_With_Unknown_Besluit_Should_Be_Invalid()
    {
        var mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext());

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();
        var mockBesluitenServiceAgent = new Mock<IBesluitenServiceAgent>();
        var mockEntityUriService = new Mock<IEntityUriService>();

        mockBesluitenServiceAgent
            .Setup(m => m.GetBesluitByUrlAsync("http://besluiten.user.local:5013/api/v1/besluiten/03018e8e-f8ab-4295-8222-842cf450a2da"))
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<BesluitResponseDto>(
                        new ErrorResponse
                        {
                            Status = (int)HttpStatusCode.NotFound,
                            Title = "Not found.",
                            Code = ErrorCode.BadUrl,
                        }
                    )
                )
            );

        var svc = new ObjectInformatieObjectBusinessRuleService(
            mockDbContext,
            mockEntityUriService.Object,
            mockZakenServiceAgent.Object,
            mockBesluitenServiceAgent.Object
        );

        var objectInformatieObject = new ObjectInformatieObject
        {
            Object = "http://besluiten.user.local:5013/api/v1/besluiten/03018e8e-f8ab-4295-8222-842cf450a2da",
            ObjectType = ObjectType.besluit,
        };

        var informatieObjectUrl = "http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/2793aeca-04e9-4b3d-8e63-2fb21a441158";

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(objectInformatieObject, informatieObjectUrl, ignoreZaakAndBesluitObjectValidation: false, errors);

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.BadUrl);

        Assert.NotNull(error);
        Assert.Equal("object", error.Name);
    }

    [Fact]
    public async Task Add_With_ServerError_Besluit_Should_Be_Invalid()
    {
        var mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext());

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();
        var mockBesluitenServiceAgent = new Mock<IBesluitenServiceAgent>();
        var mockEntityUriService = new Mock<IEntityUriService>();

        mockBesluitenServiceAgent
            .Setup(m => m.GetBesluitByUrlAsync("http://besluiten.user.local:5013/api/v1/besluiten/03018e8e-f8ab-4295-8222-842cf450a2da"))
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<BesluitResponseDto>(
                        new ErrorResponse
                        {
                            Status = (int)HttpStatusCode.InternalServerError,
                            Title = "Server error.",
                            Code = ErrorCode.BadUrl,
                        }
                    )
                )
            );

        var svc = new ObjectInformatieObjectBusinessRuleService(
            mockDbContext,
            mockEntityUriService.Object,
            mockZakenServiceAgent.Object,
            mockBesluitenServiceAgent.Object
        );

        var objectInformatieObject = new ObjectInformatieObject
        {
            Object = "http://besluiten.user.local:5013/api/v1/besluiten/03018e8e-f8ab-4295-8222-842cf450a2da",
            ObjectType = ObjectType.besluit,
        };

        var informatieObjectUrl = "http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/2793aeca-04e9-4b3d-8e63-2fb21a441158";

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(objectInformatieObject, informatieObjectUrl, ignoreZaakAndBesluitObjectValidation: false, errors);

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.BadUrl);

        Assert.NotNull(error);
        Assert.Equal("object", error.Name);
    }

    [Fact]
    public async Task Add_With_Known_Besluit_Should_Be_Valid()
    {
        var mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext());

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();
        var mockBesluitenServiceAgent = new Mock<IBesluitenServiceAgent>();
        var mockEntityUriService = new Mock<IEntityUriService>();

        mockBesluitenServiceAgent
            .Setup(m => m.GetBesluitByUrlAsync("http://besluiten.user.local:5013/api/v1/besluiten/03018e8e-f8ab-4295-8222-842cf450a2da"))
            .Returns(Task.FromResult(new ServiceAgentResponse<BesluitResponseDto>(new BesluitResponseDto())));

        mockBesluitenServiceAgent
            .Setup(m =>
                m.GetBesluitInformatieObjectenAsync(
                    It.Is<GetAllBesluitInformatieObjectenQueryParameters>(s =>
                        s.Besluit == "http://besluiten.user.local:5013/api/v1/besluiten/03018e8e-f8ab-4295-8222-842cf450a2da"
                        && s.InformatieObject
                            == "http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/2793aeca-04e9-4b3d-8e63-2fb21a441158"
                    )
                )
            )
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<IEnumerable<BesluitInformatieObjectResponseDto>>(
                        [
                            new BesluitInformatieObjectResponseDto
                            {
                                Besluit = "http://besluiten.user.local:5013/api/v1/besluiten/03018e8e-f8ab-4295-8222-842cf450a2da",
                            },
                        ]
                    )
                )
            );

        var svc = new ObjectInformatieObjectBusinessRuleService(
            mockDbContext,
            mockEntityUriService.Object,
            mockZakenServiceAgent.Object,
            mockBesluitenServiceAgent.Object
        );

        var objectInformatieObject = new ObjectInformatieObject
        {
            Object = "http://besluiten.user.local:5013/api/v1/besluiten/03018e8e-f8ab-4295-8222-842cf450a2da",
            ObjectType = ObjectType.besluit,
        };

        var informatieObjectUrl = "http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/2793aeca-04e9-4b3d-8e63-2fb21a441158";

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(objectInformatieObject, informatieObjectUrl, ignoreZaakAndBesluitObjectValidation: false, errors);

        Assert.True(valid);
    }

    private static async Task<DbContextOptions<DrcDbContext>> GetMockedDrcDbContext()
    {
        var options = new DbContextOptionsBuilder<DrcDbContext>().UseInMemoryDatabase(databaseName: $"drc-{Guid.NewGuid()}").Options;

        // Insert seed data into the database using one instance of the context
        using (var context = new UnitTestDrcDbContext(options))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.ObjectInformatieObjecten.Add(
                new ObjectInformatieObject
                {
                    InformatieObject = new EnkelvoudigInformatieObject
                    {
                        Id = Guid.Parse("2793aeca-04e9-4b3d-8e63-2fb21a441158"),
                        InformatieObjectType = string.Empty,
                        Owner = "000000000",
                    },
                    Object = "/zaken/03018e8e-f8ab-4295-8222-842cf450a2da",
                    Owner = "000000000",
                }
            );

            await context.SaveChangesAsync();
        }

        return options;
    }
}
