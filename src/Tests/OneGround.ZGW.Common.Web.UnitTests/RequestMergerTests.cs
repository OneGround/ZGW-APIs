using System;
using System.Collections.Generic;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests;

class Entity<T> : IBaseEntity
{
    public Guid Id
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public T Property { get; set; }
}

class RequestDto<T>
{
    public Guid Id { get; set; }
    public T Property { get; set; }
}

public class RequestMergerTests : IDisposable
{
    private readonly JsonSerializer _serializer = new ZGWJsonSerializer();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly RequestMerger _merger;

    public RequestMergerTests()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(RequestMergerTests).Assembly);
        _provider = services.BuildServiceProvider();

        // Configured on this test's own config, never inside AddZgwMapster: Entity<T>.Id throws on get, so
        // the destination Id must be ignored or the map dereferences it. RequireDestinationMemberSource is
        // Mapster's stand-in for AutoMapper's AssertConfigurationIsValid -- if these tests start failing,
        // that means a mapping is missing an Ignore or an explicit Map for a member that does not map
        // automatically by name.
        var config = _provider.GetRequiredService<TypeAdapterConfig>();

        // Geometry passthrough, for the same reason RequestToDomainRegister registers one for the abstract
        // Geometry base: Mapster's same-type clone expression is wrong for NetTopologySuite geometry. Point
        // is concrete, so unlike Geometry it clones without erroring -- it just walks Boundary/Centroid/
        // Envelope and every other computed geometry property, and compilation never terminates. Measured:
        // without this rule config.Compile() below hangs indefinitely rather than throwing.
        config.NewConfig<Point, Point>().MapWith(src => src);

        config.ForType<Entity<bool>, RequestDto<bool>>().Ignore(dest => dest.Id).RequireDestinationMemberSource(true);
        config.ForType<Entity<string>, RequestDto<string>>().Ignore(dest => dest.Id).RequireDestinationMemberSource(true);
        config.ForType<Entity<Point>, RequestDto<Point>>().Ignore(dest => dest.Id).RequireDestinationMemberSource(true);
        config.Compile();

        // ServiceMapper is scoped and resolves the url resolver through the request's provider, so the
        // scope has to outlive every test in this class, not just the constructor.
        _scope = _provider.CreateScope();
        _merger = new RequestMerger(_scope.ServiceProvider.GetRequiredService<IMapper>());
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }

    public static IEnumerable<object[]> TypeTestData =>
        [
            ["value"],
            [true],
            [new Point(11.1, 12.2)],
        ];

    public static IEnumerable<object[]> ValueTestData =>
        [
            ["value", "new_value"],
            [true, false],
            [new Point(11.1, 12.2), new Point(13.3, 14.4)],
        ];

    private JObject CreateJObject(object o) => JObject.FromObject(o, _serializer);

    [Theory]
    [MemberData(nameof(TypeTestData))]
    public void EmptyObject_DoesNotChangeProperty<T>(T value)
    {
        var entity = new Entity<T> { Property = value };

        var result = _merger.MergePartialUpdateToObjectRequest<RequestDto<T>, Entity<T>>(entity, CreateJObject(new { }));

        Assert.Equal(entity.Property, result.Property);
    }

    [Theory]
    [MemberData(nameof(ValueTestData))]
    public void PropertyInObject_ChangesProperty<T>(T value, T new_value)
    {
        var entity = new Entity<T> { Property = value };

        var result = _merger.MergePartialUpdateToObjectRequest<RequestDto<T>, Entity<T>>(entity, CreateJObject(new { property = new_value }));

        Assert.Equal(new_value, result.Property);
    }

    [Theory]
    [MemberData(nameof(TypeTestData))]
    public void NullPropertyInObject_NullsProperty<T>(T value)
    {
        var entity = new Entity<T> { Property = value };

        var result = _merger.MergePartialUpdateToObjectRequest<RequestDto<T>, Entity<T>>(entity, CreateJObject(new { property = default(T) }));

        Assert.Equal(default, result.Property);
    }
}
