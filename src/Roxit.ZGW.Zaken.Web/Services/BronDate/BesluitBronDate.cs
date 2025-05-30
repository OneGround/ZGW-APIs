using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Besluiten.Contracts.v1.Responses;
using Roxit.ZGW.Besluiten.ServiceAgent.v1;

namespace Roxit.ZGW.Zaken.Web.Services.BronDate;

public abstract class BesluitBronDate
{
    private readonly IBesluitenServiceAgent _besluitenServiceAgent;

    public BesluitBronDate(IBesluitenServiceAgent besluitenServiceAgent)
    {
        _besluitenServiceAgent = besluitenServiceAgent;
    }

    protected async Task<BesluitResponseDto> GetBesluitAsync(string besluitUrl, List<ArchiveValidationError> errors)
    {
        var result = await _besluitenServiceAgent.GetBesluitByUrlAsync(besluitUrl);

        if (!result.Success)
        {
            errors.Add(new ArchiveValidationError("object", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }
}
