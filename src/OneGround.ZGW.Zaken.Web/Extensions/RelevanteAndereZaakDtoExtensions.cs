using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.Zaken.Contracts.v1;

namespace OneGround.ZGW.Zaken.Web.Extensions;

public static class RelevanteAndereZaakDtoExtensions
{
    /// <summary>
    /// The urls of the relevanteAndereZaken as the client sent them (the business rules validate these; the relation itself is stored by the id of
    /// the referenced zaak). A missing list gives an empty list.
    /// </summary>
    public static IReadOnlyCollection<string> GetUrls(this IEnumerable<RelevanteAndereZaakDto> relevanteAndereZaken) =>
        relevanteAndereZaken?.Select(r => r.Url).ToList() ?? [];
}
