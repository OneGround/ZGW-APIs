using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Common.Contracts.v1;

namespace Roxit.ZGW.Autorisaties.Common.BusinessRules;

public interface IApplicatieBusinessRuleService
{
    Task<bool> ValidateAddAsync(Applicatie applicatie, List<ValidationError> errors, bool checkComponentUrl = true);
    Task<bool> ValidateUpdateAsync(Applicatie existingApp, Applicatie newApp, List<ValidationError> errors, bool checkComponentUrl = true);
    Task<bool> ValidateApplicatieAsync(Applicatie applicatie, List<ValidationError> errors);
}
