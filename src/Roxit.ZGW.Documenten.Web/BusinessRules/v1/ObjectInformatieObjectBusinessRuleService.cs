using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roxit.ZGW.Besluiten.Contracts.v1.Queries;
using Roxit.ZGW.Besluiten.ServiceAgent.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;
using Roxit.ZGW.Zaken.ServiceAgent.v1;

namespace Roxit.ZGW.Documenten.Web.BusinessRules.v1;

public interface IObjectInformatieObjectBusinessRuleService
{
    Task<bool> ValidateAsync(
        ObjectInformatieObject objectInformatieObjectAdd,
        string informatieObjectUrl,
        bool ignoreZaakAndBesluitObjectValidation,
        List<ValidationError> errors,
        CancellationToken cancellationToken = default
    );
}

public class ObjectInformatieObjectBusinessRuleService : IObjectInformatieObjectBusinessRuleService
{
    private readonly DrcDbContext _context;
    private readonly IEntityUriService _uriService;
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly IBesluitenServiceAgent _besluitenServiceAgent;

    public ObjectInformatieObjectBusinessRuleService(
        DrcDbContext context,
        IEntityUriService uriService,
        IZakenServiceAgent zakenServiceAgent,
        IBesluitenServiceAgent besluitenServiceAgent
    )
    {
        _context = context;
        _uriService = uriService;
        _zakenServiceAgent = zakenServiceAgent;
        _besluitenServiceAgent = besluitenServiceAgent;
    }

    public async Task<bool> ValidateAsync(
        ObjectInformatieObject objectInformatieObject,
        string informatieObjectUrl,
        bool ignoreZaakAndBesluitObjectValidation,
        List<ValidationError> errors,
        CancellationToken cancellationToken = default
    )
    {
        // Note: Valideren uniciteit combinatie object en informatieobject op de ObjectInformatieObject-resource (drc-003)
        if (
            await _context
                .ObjectInformatieObjecten.AsNoTracking()
                .Include(z => z.InformatieObject)
                .AnyAsync(
                    z => z.InformatieObject.Id == _uriService.GetId(informatieObjectUrl) && z.Object == objectInformatieObject.Object,
                    cancellationToken
                )
        )
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.InconsistentRelation, "De combinatie informatieobject en object bestaat al.");

            errors.Add(error);
        }

        if (!ignoreZaakAndBesluitObjectValidation)
        {
            if (objectInformatieObject.ObjectType == ObjectType.zaak)
            {
                // Note: Valideren object op de ObjectInformatieObject-resource (drc-002)
                await ValidateZaakAsync(objectInformatieObject.Object, errors);
                if (errors.Count == 0)
                {
                    // Note: Valideren bestaan relatie tussen object en informatieobject in de bron (drc-004)
                    await ValidateZaakRelatieObjectAndInformatieObjectAsync(objectInformatieObject.Object, informatieObjectUrl, errors);
                }
            }
            else if (objectInformatieObject.ObjectType == ObjectType.besluit)
            {
                // Note: Valideren object op de ObjectInformatieObject-resource (drc-002)
                await ValidateBesluitAsync(objectInformatieObject.Object, errors);
                if (errors.Count == 0)
                {
                    // Note: Valideren bestaan relatie tussen object en informatieobject in de bron (drc-004)
                    await ValidateBesluitRelatieObjectAndInformatieObjectAsync(objectInformatieObject.Object, informatieObjectUrl, errors);
                }
            }
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateZaakAsync(string zaakObject, List<ValidationError> errors)
    {
        // Note: Valideren object op de ObjectInformatieObject-resource (drc-002)

        var result = await _zakenServiceAgent.GetZaakByUrlAsync(zaakObject);

        if (!result.Success)
        {
            errors.Add(new ValidationError("object", result.Error.Code, result.Error.Title));
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateBesluitAsync(string besluitObject, List<ValidationError> errors)
    {
        // Note: Valideren object op de ObjectInformatieObject-resource (drc-002)

        var result = await _besluitenServiceAgent.GetBesluitByUrlAsync(besluitObject);

        if (!result.Success)
        {
            errors.Add(new ValidationError("object", result.Error.Code, result.Error.Title));
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateZaakRelatieObjectAndInformatieObjectAsync(
        string zaakObject,
        string informatieObject,
        List<ValidationError> errors
    )
    {
        // Note: Valideren bestaan relatie tussen object en informatieobject in de bron (drc-004)
        var parameters = new GetAllZaakInformatieObjectenQueryParameters { InformatieObject = informatieObject, Zaak = zaakObject };
        var result = await _zakenServiceAgent.GetZaakInformatieObjectenAsync(parameters);
        if (!result.Success)
        {
            errors.Add(new ValidationError("object", result.Error.Code, result.Error.Title));
        }

        if (!result.Response.Any())
        {
            string error = "Het informatieobject is in het ZRC (Zaken) nog niet gerelateerd aan het object.";

            errors.Add(new ValidationError("nonFieldErrors", ErrorCode.InconsistentRelation, error));
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateBesluitRelatieObjectAndInformatieObjectAsync(
        string besluitObject,
        string informatieObject,
        List<ValidationError> errors
    )
    {
        // Note: Valideren bestaan relatie tussen object en informatieobject in de bron (drc-004)
        var parameters = new GetAllBesluitInformatieObjectenQueryParameters { InformatieObject = informatieObject, Besluit = besluitObject };
        var result = await _besluitenServiceAgent.GetBesluitInformatieObjectenAsync(parameters);
        if (!result.Success)
        {
            errors.Add(new ValidationError("object", result.Error.Code, result.Error.Title));
        }

        if (result.Response.Any())
            return errors.Count == 0;

        const string error = "Het informatieobject is in het BRC (Besluiten) nog niet gerelateerd aan het object.";

        errors.Add(new ValidationError("nonFieldErrors", ErrorCode.Unique, error));

        return errors.Count == 0;
    }
}
