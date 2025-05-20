using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace OneGround.ZGW.Documenten.Services;

public class DocumentServicesResolver : IDocumentServicesResolver
{
    private readonly IEnumerable<IDocumentService> _registeredDocumentServices;
    private readonly string _default;

    public DocumentServicesResolver(IConfiguration configuration, IEnumerable<IDocumentService> registeredDocumentServices)
    {
        const string defaultDocumentenService = "Application:DefaultDocumentenService";

        _registeredDocumentServices = registeredDocumentServices;

        _default = configuration.GetSection(defaultDocumentenService).Value;
        if (_default == null)
            throw new InvalidOperationException($"Default DocumentenService not specified in appsettings '{defaultDocumentenService}'.");
    }

    public IDocumentService GetDefault()
    {
        return Find(_default);
    }

    public IDocumentService Find(string providerPrefix)
    {
        var service = _registeredDocumentServices.SingleOrDefault(s => providerPrefix == s.ProviderPrefix);

        return service;
    }
}
