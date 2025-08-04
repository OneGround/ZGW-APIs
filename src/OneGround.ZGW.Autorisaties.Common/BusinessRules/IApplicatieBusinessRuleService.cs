using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Common.Contracts.v1;

namespace OneGround.ZGW.Autorisaties.Common.BusinessRules;

public interface IApplicatieBusinessRuleService
{
    Task<bool> ValidateAddAsync(Applicatie applicatie, List<ValidationError> errors, bool checkComponentUrl = true);
    Task<bool> ValidateUpdateAsync(Applicatie existingApp, Applicatie newApp, List<ValidationError> errors, bool checkComponentUrl = true);
}
