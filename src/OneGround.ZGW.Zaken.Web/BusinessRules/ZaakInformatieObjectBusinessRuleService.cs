using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.ServiceAgent.v1.Extensions;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.BusinessRules;

public class ZaakInformatieObjectBusinessRuleService : IZaakInformatieObjectBusinessRuleService
{
    private readonly ZrcDbContext _context;
    private readonly IEntityUriService _uriService;
    private readonly ICachedDocumentenServiceAgent _documentenServiceAgent;

    public ZaakInformatieObjectBusinessRuleService(
        ZrcDbContext context,
        IEntityUriService uriService,
        ICachedDocumentenServiceAgent documentenServiceAgent
    )
    {
        _context = context;
        _uriService = uriService;
        _documentenServiceAgent = documentenServiceAgent;
    }

    public async Task<bool> ValidateAsync(
        ZaakInformatieObject zaakInformatieObjectAdd,
        string zaakUrl,
        bool ignoreInformatieObjectValidation,
        List<ValidationError> errors
    )
    {
        // Add rules
        if (
            await _context
                .ZaakInformatieObjecten.AsNoTracking()
                .Include(z => z.Zaak)
                .AnyAsync(z => z.Zaak.Id == _uriService.GetId(zaakUrl) && z.InformatieObject == zaakInformatieObjectAdd.InformatieObject)
        )
        {
            var error = new ValidationError("informatieobject", ErrorCode.Unique, "Dit informatieobject bestaat al voor deze zaak.");
            errors.Add(error);
        }

        if (!ignoreInformatieObjectValidation)
        {
            await ValidateInformatieObjectAsync(zaakInformatieObjectAdd.InformatieObject, errors);
        }

        return errors.Count == 0;
    }

    public Task<bool> ValidateAsync(
        ZaakInformatieObject zaakInformatieObjectExisting,
        ZaakInformatieObject zaakInformatieObjectUpdate,
        string zaakUrl,
        List<ValidationError> errors
    )
    {
        // Update rules
        if (zaakInformatieObjectExisting.Zaak.Id != _uriService.GetId(zaakUrl))
        {
            var error = new ValidationError("zaak", ErrorCode.UpdateNotAllowed, "Dit veld mag niet gewijzigd worden.");

            errors.Add(error);
        }

        if (zaakInformatieObjectExisting.InformatieObject != zaakInformatieObjectUpdate.InformatieObject)
        {
            var error = new ValidationError("informatieobject", ErrorCode.UpdateNotAllowed, "Dit veld mag niet gewijzigd worden.");

            errors.Add(error);
        }

        return Task.FromResult(errors.Count == 0);
    }

    private async Task<bool> ValidateInformatieObjectAsync(string informatieObject, List<ValidationError> errors)
    {
        var result = await _documentenServiceAgent.GetEnkelvoudigInformatieObjectByUrlAsync(informatieObject);
        if (!result.Success)
        {
            errors.Add(new ValidationError("informatieobject", result.Error.Code, result.Error.Title));
        }

        return errors.Count == 0;
    }
}
