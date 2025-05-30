using System;
using System.Collections.Generic;
using AutoMapper;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.DataAccess;
using Xunit;

namespace Roxit.ZGW.Common.Web.UnitTests;

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

public class RequestMergerTests
{
    private readonly JsonSerializer _serializer = new ZGWJsonSerializer();
    private readonly RequestMerger _merger;

    public RequestMergerTests()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.CreateMap<Entity<bool>, RequestDto<bool>>().ForMember(dest => dest.Id, opt => opt.Ignore());
            config.CreateMap<Entity<string>, RequestDto<string>>().ForMember(dest => dest.Id, opt => opt.Ignore());
            config.CreateMap<Entity<Point>, RequestDto<Point>>().ForMember(dest => dest.Id, opt => opt.Ignore());
        });

        // Important: if tests starts failing, that means that mappings are missing Ignore() or MapFrom()
        // for members which does not map automatically by name
        configuration.AssertConfigurationIsValid();

        _merger = new RequestMerger(configuration.CreateMapper());
    }

    public static IEnumerable<object[]> TypeTestData =>
        [
            //new object [] { "value" },
            //new object [] { true },
            [new Point(11.1, 12.2)],
        ];

    public static IEnumerable<object[]> ValueTestData =>
        [
            //new object [] { "value", "new_value" },
            //new object [] { true, false },
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
