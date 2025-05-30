using System;
using System.Collections.Generic;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.Contracts.v1._5.Responses;
using Roxit.ZGW.Zaken.Web.Handlers.v1._5;

namespace Roxit.ZGW.Zaken.Web.Expands.v1._5;

public abstract class ZaakBaseExpander
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEntityUriService _uriService;

    public ZaakBaseExpander(IServiceProvider serviceProvider, IEntityUriService uriService)
    {
        _serviceProvider = serviceProvider;
        _uriService = uriService;
    }

    protected ZaakResponseDto GetZaak(string zaakUrl, out object error)
    {
        error = new object();

        using var scope = _serviceProvider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mediator.Send(new GetZaakQuery { Id = _uriService.GetId(zaakUrl) }).Result;
        if (result.Status == QueryStatus.NotFound)
        {
            error = ExpandError.Create($"Zaak {zaakUrl} niet gevonden.");
            return null;
        }

        if (result.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result.Errors);
            return null;
        }

        var zaakDto = mapper.Map<ZaakResponseDto>(result.Result);

        return zaakDto;
    }

    protected static HashSet<string> GetInnerExpandLookup(string outerExpandName, HashSet<string> expandLookup)
    {
        var innerExpandLookup = new HashSet<string>();
        foreach (var expand in expandLookup)
        {
            if (expand.StartsWith(outerExpandName))
            {
                var innerExpand = expand.Replace(outerExpandName, "").Trim('.');
                if (!string.IsNullOrWhiteSpace(innerExpand))
                {
                    innerExpandLookup.Add(innerExpand);
                }
            }
        }
        return innerExpandLookup;
    }
}
