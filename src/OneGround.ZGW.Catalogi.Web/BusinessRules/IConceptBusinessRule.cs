using System.Collections.Generic;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;

namespace OneGround.ZGW.Catalogi.Web.BusinessRules;

public interface IConceptBusinessRule
{
    bool ValidateConcept(IConceptEntity entity, List<ValidationError> errors);
    bool ValidateConceptZaakType(IConceptEntity entity, List<ValidationError> errors);
    bool ValidateConceptRelation(IConceptEntity entity, List<ValidationError> errors, decimal version);
    bool ValidateNonConceptRelation(IConceptEntity entity, List<ValidationError> errors);
    bool ValidateGeldigheid(List<IConceptEntity> entities, IConceptEntity entity, List<ValidationError> errors);
}
