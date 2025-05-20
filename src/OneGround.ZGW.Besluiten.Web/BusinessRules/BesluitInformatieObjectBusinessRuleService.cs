using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Documenten.ServiceAgent.v1;

namespace OneGround.ZGW.Besluiten.Web.BusinessRules;

public class BesluitInformatieObjectBusinessRuleService : IBesluitInformatieObjectBusinessRuleService
{
    private readonly BrcDbContext _context;
    private readonly IDocumentenServiceAgent _documentenServiceAgent;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;

    public BesluitInformatieObjectBusinessRuleService(
        BrcDbContext context,
        IDocumentenServiceAgent documentenServiceAgent,
        ICatalogiServiceAgent catalogiServiceAgent
    )
    {
        _context = context;
        _documentenServiceAgent = documentenServiceAgent;
        _catalogiServiceAgent = catalogiServiceAgent;
    }

    public async Task<bool> ValidateAsync(
        Besluit besluit,
        BesluitInformatieObject besluitInformatieObjectAdd,
        bool ignoreInformatieObjectValidation,
        List<ValidationError> errors
    )
    {
        if (
            await _context
                .BesluitInformatieObjecten.AsNoTracking()
                .Include(z => z.Besluit)
                .AnyAsync(z => z.Besluit.Id == besluit.Id && z.InformatieObject == besluitInformatieObjectAdd.InformatieObject)
        )
        {
            var error = new ValidationError("informatieobject", ErrorCode.Unique, "Dit informatieobject bestaat al voor dit besluit.");

            errors.Add(error);
        }

        if (!ignoreInformatieObjectValidation)
        {
            await ValidateInformatieObjectAsync(besluit, besluitInformatieObjectAdd, errors);
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateInformatieObjectAsync(
        Besluit besluit,
        BesluitInformatieObject besluitInformatieObjectAdd,
        List<ValidationError> errors
    )
    {
        var result = await _documentenServiceAgent.GetEnkelvoudigInformatieObjectByUrlAsync(besluitInformatieObjectAdd.InformatieObject);
        if (!result.Success)
        {
            errors.Add(new ValidationError("informatieobject", result.Error.Code, result.Error.Title));
        }
        else
        {
            // Valideren dat het informatieobjecttype van een BesluitInformatieObject bij het Besluit.besluittype hoort (brc-007)
            var besluittype = await _catalogiServiceAgent.GetBesluitTypeByUrlAsync(besluit.BesluitType);

            if (!besluittype.Response.InformatieObjectTypen.Any(t => t == result.Response.InformatieObjectType))
            {
                string error = "De referentie hoort niet bij het besluittype van het informatieobject.";

                errors.Add(new ValidationError("nonFieldErrors", ErrorCode.MissingBesluitTypeInformatieObjectTypeRelation, error));
            }
        }

        return errors.Count == 0;
    }
}
