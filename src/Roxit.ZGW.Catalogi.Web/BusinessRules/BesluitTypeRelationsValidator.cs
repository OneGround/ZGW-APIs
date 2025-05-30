using System;
using System.Collections.Generic;
using System.Linq;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.BusinessRules;

public interface IBesluitTypeRelationsValidator
{
    /// <summary>
    /// The orignal relations must be in the patched collection of relations
    /// </summary>
    bool Validate(IEnumerable<Guid> existingRelations, IEnumerable<string> patchUrlRelations);
}

public class BesluitTypeRelationsValidator : IBesluitTypeRelationsValidator
{
    private readonly IEntityUriService _uriService;

    public BesluitTypeRelationsValidator(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public bool Validate(IEnumerable<Guid> existingRelations, IEnumerable<string> patchUrlRelations)
    {
        var patchRelations = patchUrlRelations.Select(url => _uriService.GetId(url)).ToArray();

        return existingRelations.All(r => patchRelations.Contains(r));
    }
}
