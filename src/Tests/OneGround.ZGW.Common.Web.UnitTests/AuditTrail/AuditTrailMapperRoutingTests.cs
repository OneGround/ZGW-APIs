using System;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Moq;
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

    private static AuditTrailService CreateSut(IMapper mapper) =>
        new AuditTrailService(Mock.Of<IDbContextWithAuditTrail>(), mapper, Mock.Of<IHttpContextAccessor>(), Mock.Of<IEntityUriService>());

    [Fact]
    public void SetNew_maps_through_the_injected_mapper()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<TestDto>(entity)).Returns(new TestDto { Naam = "gemapt" });

        CreateSut(mapper.Object).SetNew<TestDto>(entity);

        // Proves the audit trail builds its DTO through the injected mapper rather than serializing the
        // entity itself -- the reason a register's rules govern what an audit record contains.
        mapper.Verify(m => m.Map<TestDto>(entity), Times.Once());
    }

    [Fact]
    public void SetOld_maps_through_the_injected_mapper()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<TestDto>(entity)).Returns(new TestDto { Naam = "gemapt" });

        CreateSut(mapper.Object).SetOld<TestDto>(entity);

        mapper.Verify(m => m.Map<TestDto>(entity), Times.Once());
    }
}
