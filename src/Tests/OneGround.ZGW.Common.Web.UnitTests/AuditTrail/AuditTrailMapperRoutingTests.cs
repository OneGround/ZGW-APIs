using System;
using Microsoft.AspNetCore.Http;
using Moq;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.AuditTrail;

public class AuditTrailMapperRoutingTests
{
    private sealed class TestEntity : IBaseEntity
    {
        public Guid Id { get; set; }
    }

    private sealed class TestDto
    {
        public string Naam { get; set; }
    }

    private static AuditTrailService CreateSut(IZgwMapper mapper) =>
        new AuditTrailService(Mock.Of<IDbContextWithAuditTrail>(), mapper, Mock.Of<IHttpContextAccessor>(), Mock.Of<IEntityUriService>());

    [Fact]
    public void SetNew_maps_through_the_injected_IZgwMapper()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var mapper = new Mock<IZgwMapper>();
        mapper.Setup(m => m.Map<TestDto>(entity)).Returns(new TestDto { Naam = "gemapt" });

        CreateSut(mapper.Object).SetNew<TestDto>(entity);

        // Proves the audit trail resolves its DTO through the swappable abstraction rather than a
        // hard-coded mapper, which is what lets a service choose Mapster without touching this class.
        mapper.Verify(m => m.Map<TestDto>(entity), Times.Once());
    }

    [Fact]
    public void SetOld_maps_through_the_injected_IZgwMapper()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var mapper = new Mock<IZgwMapper>();
        mapper.Setup(m => m.Map<TestDto>(entity)).Returns(new TestDto { Naam = "gemapt" });

        CreateSut(mapper.Object).SetOld<TestDto>(entity);

        mapper.Verify(m => m.Map<TestDto>(entity), Times.Once());
    }
}
