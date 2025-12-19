using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.Web.Handlers.v1._5;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._5;

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

    public Task<object> ResolveAsync(IExpandParser expandLookup, InformatieObjectContext entity)
    {
        // Note: Not called directly so we can keep as it is now
        throw new NotImplementedException();
    }

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, InformatieObjectContext context)
    {
        if (
            expandLookup.ContainsAnyOf("informatieobject", "informatieobject.informatieobjecttype", "informatieobject.informatieobjecttype.catalogus")
        )
        {
            object error = null;

            var enkelvoudiginformatieobjectDto = await _enkelvoudiginformatieobjectCache.GetOrCacheAndGetAsync(
                $"key_enkelvoudiginformatieobject_{context.InformatieObject}",
                async () =>
                {
                    using var scope = _serviceProvider.CreateScope();

                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

                    var result = await mediator.Send(new GetEnkelvoudigInformatieObjectQuery { Id = _uriService.GetId(context.InformatieObject) });
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

            if (enkelvoudiginformatieobjectDto != null)
            {
                var expander = _expanderFactory.Create<EnkelvoudigInformatieObjectGetResponseDto>("enkelvoudiginformatieobject");

                var enkelvoudiginformatieobjectDtoExpanded = await expander.ResolveAsync(expandLookup, enkelvoudiginformatieobjectDto);

                var objectDtoExpanded = DtoExpander.Merge(
                    context.ObjectDto,
                    new { _expand = new { informatieobject = enkelvoudiginformatieobjectDtoExpanded ?? error ?? new object() } }
                );

                return objectDtoExpanded;
            }
            else
            {
                var objectDtoExpanded = DtoExpander.Merge(context.ObjectDto, new { _expand = new { informatieobject = error ?? new object() } });

                return objectDtoExpanded;
            }
        }

        return context.ObjectDto;
    }
}
