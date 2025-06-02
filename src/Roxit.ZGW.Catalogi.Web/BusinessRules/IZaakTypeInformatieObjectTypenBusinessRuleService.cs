using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Contracts.v1;

namespace Roxit.ZGW.Catalogi.Web.BusinessRules;

public interface IZaakTypeInformatieObjectTypenBusinessRuleService
{
    Task<bool> ValidateAsync(
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
    );

    Task<bool> ValidateExistsAsync(
        Guid zaakTypeId,
        string informatieObjectTypeOmschrijving,
        int volgnummer,
        Richting richting,
        List<ValidationError> errors
    );

    Task<bool> ValidateExistsAsync(
        Guid? existingZaaktypeInformatieobjectTypeId,
        Guid zaakTypeId,
        string informatieObjectTypeOmschrijving,
        int volgnummer,
        Richting richting,
        List<ValidationError> errors
    );
}
