using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Contracts.v1._5.Responses;
using Roxit.ZGW.Documenten.Web.Handlers.v1._5;

namespace Roxit.ZGW.Documenten.Web.Expands.v1._5;

public class InformatieObjectContext
{
    public string InformatieObject { get; set; }
    public object ObjectDto { get; set; }
}

// Note: This InformatieObjectExpander expand context.ObjectDto with informatieobject or informatieobject.informatieobjecttype
public class InformatieObjectExpander : IObjectExpander<InformatieObjectContext>
{
    private readonly IExpanderFactory _expanderFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEntityUriService _uriService;
    private readonly IGenericCache<EnkelvoudigInformatieObjectGetResponseDto> _enkelvoudiginformatieobjectCache;

    public InformatieObjectExpander(
        IExpanderFactory expanderFactory,
        IServiceProvider serviceProvider,
        IEntityUriService uriService,
        IGenericCache<EnkelvoudigInformatieObjectGetResponseDto> enkelvoudiginformatieobjectCache
    )
    {
        _expanderFactory = expanderFactory;
        _serviceProvider = serviceProvider;
        _uriService = uriService;
        _enkelvoudiginformatieobjectCache = enkelvoudiginformatieobjectCache;
    }

    public string ExpandName => "informatieobject";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, InformatieObjectContext context)
    {
        if (
            expandLookup.ContainsAnyOf("informatieobject", "informatieobject.informatieobjecttype", "informatieobject.informatieobjecttype.catalogus")
        )
        {
            object error = null;

            var enkelvoudiginformatieobjectDto = _enkelvoudiginformatieobjectCache.GetOrCacheAndGet(
                $"key_enkelvoudiginformatieobject_{context.InformatieObject}",
                () =>
                {
                    using var scope = _serviceProvider.CreateScope();

                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

                    var result = mediator.Send(new GetEnkelvoudigInformatieObjectQuery { Id = _uriService.GetId(context.InformatieObject) }).Result;
                    if (result.Status == QueryStatus.NotFound)
                    {
                        error = ExpandError.Create("Enkelvoudiginformatieobject niet gevonden."); // Should never be landed here
                        return null;
                    }

                    if (result.Status != QueryStatus.OK)
                    {
                        error = ExpandError.Create(result.Errors);
                        return null;
                    }

                    var _enkelvoudiginformatieobjectDto = mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(result.Result);

                    return _enkelvoudiginformatieobjectDto;
                }
            );

            var expander = _expanderFactory.Create<EnkelvoudigInformatieObjectGetResponseDto>("enkelvoudiginformatieobject");

            var enkelvoudiginformatieobjectDtoExpanded = await expander.ResolveAsync(expandLookup, enkelvoudiginformatieobjectDto);

            var objectDtoExpanded = DtoExpander.Merge(
                context.ObjectDto,
                new { _expand = new { informatieobject = enkelvoudiginformatieobjectDtoExpanded ?? error ?? new object() } }
            );

            return objectDtoExpanded;
        }

        return context.ObjectDto;
    }
}
