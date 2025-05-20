using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.BusinessRules;

public class ZaakTypeInformatieObjectTypenBusinessRuleService : IZaakTypeInformatieObjectTypenBusinessRuleService
{
    private readonly ZtcDbContext _context;
    private readonly IEntityUriService _uriService;
    private readonly IConceptBusinessRule _conceptBusinessRule;

    public ZaakTypeInformatieObjectTypenBusinessRuleService(
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule
    )
    {
        _context = context;
        _uriService = uriService;
        _conceptBusinessRule = conceptBusinessRule;
    }

    public Task<bool> ValidateExistsAsync(
        Guid zaakTypeId,
        string informatieObjectTypeOmschrijving,
        int volgnummer,
        Richting richting,
        List<ValidationError> errors
    )
    {
        return ValidateExistsAsync(
            existingZaaktypeInformatieobjectTypeId: null,
            zaakTypeId,
            informatieObjectTypeOmschrijving,
            volgnummer,
            richting,
            errors
        );
    }

    public async Task<bool> ValidateExistsAsync(
        Guid? existingZaaktypeInformatieobjectTypeId,
        Guid zaakTypeId,
        string informatieObjectTypeOmschrijving,
        int volgnummer,
        Richting richting,
        List<ValidationError> errors
    )
    {
        var recordExists = await _context
            .ZaakTypeInformatieObjectTypen.AsNoTracking()
            .AnyAsync(i =>
                (!existingZaaktypeInformatieobjectTypeId.HasValue || existingZaaktypeInformatieobjectTypeId.Value != i.Id)
                && i.ZaakTypeId == zaakTypeId
                && i.InformatieObjectTypeOmschrijving == informatieObjectTypeOmschrijving
                && i.VolgNummer == volgnummer
                && i.Richting == richting
            );
        if (recordExists)
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.Unique,
                "De velden zaaktype, informatieobjecttype en volgnummer moeten een unieke set zijn."
            );

            errors.Add(error);
        }
        return errors.Count == 0;
    }

    public async Task<bool> ValidateAsync(
        string zaakTypeUrl,
        string informatieObjectTypeContext,
        List<ValidationError> errors,
        Expression<Func<ZaakType, bool>> zaakTypeFilter,
        Expression<Func<InformatieObjectType, bool>> informatieObjectTypeFilter,
        Expression<Func<StatusType, bool>> statusTypeFilter,
        bool ignoreZaakType,
        bool ignoreStatusType,
        bool ignoreInformatieObjectType,
        bool ignoreBusinessRuleStatustypeZaaktypeValidation,
        decimal version,
        string statusTypeUrl = null
    )
    {
        var zaakType = default(ZaakType);
        if (!ignoreZaakType)
        {
            zaakType = await _context
                .ZaakTypen.AsNoTracking()
                .Where(zaakTypeFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(zaakTypeUrl));

            if (zaakType == null)
            {
                var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{zaakTypeUrl}' niet gevonden.");
                errors.Add(error);
            }
        }

        var informatieObjectType = default(InformatieObjectType);
        if (zaakType != null && !ignoreInformatieObjectType)
        {
            informatieObjectType = await _context
                .InformatieObjectTypen.AsNoTracking()
                .Where(informatieObjectTypeFilter)
                .SingleOrDefaultAsync(i => i.Id == _uriService.GetId(informatieObjectTypeContext));

            if (informatieObjectType == null)
            {
                var error = new ValidationError(
                    "informatieobjecttype",
                    ErrorCode.NotFound,
                    $"InformatieObjectType '{informatieObjectTypeContext}' niet gevonden."
                );
                errors.Add(error);
            }
        }

        if (zaakType != null && informatieObjectType != null)
        {
            if (zaakType.CatalogusId != informatieObjectType.CatalogusId)
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.RelationsIncorrectCatalogus,
                    "The zaaktype has catalogus different from informatieobjecttype."
                );
                errors.Add(error);
            }

            if (!ignoreInformatieObjectType && !ignoreZaakType)
            {
                var err = new List<ValidationError>();
                _conceptBusinessRule.ValidateConceptRelation(zaakType, err, version);
                _conceptBusinessRule.ValidateConceptRelation(informatieObjectType, err, version);
                if (err.Count > 0)
                {
                    errors.Add(err.Last());
                }
            }
        }

        if (!ignoreStatusType)
        {
            if (statusTypeUrl != null)
            {
                var statusType = await _context
                    .StatusTypen.AsNoTracking()
                    .Include(s => s.ZaakType)
                    .Where(statusTypeFilter)
                    .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(statusTypeUrl));

                if (statusType == null)
                {
                    var error = new ValidationError("statusType", ErrorCode.NotFound, $"StatusType '{statusTypeUrl}' niet gevonden.");

                    errors.Add(error);
                }
                else if (!ignoreBusinessRuleStatustypeZaaktypeValidation && zaakType != null && statusType.ZaakType.Url != zaakType.Url)
                {
                    var error = new ValidationError(
                        "statusType",
                        ErrorCode.Invalid,
                        $"StatusType '{statusTypeUrl}' belongs not to the specified zaaktype of the request."
                    );

                    errors.Add(error);
                }
            }
        }

        return errors.Count == 0;
    }
}
