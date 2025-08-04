using System;
using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Notificaties.DataModel;

namespace OneGround.ZGW.Notificaties.Web.Validators;

public interface IAbonnementKanaalValidator
{
    bool Validate(AbonnementKanaal abonnementkanaal, Kanaal kanaal, List<ValidationError> errors);
}

public class AbonnementKanaalValidator : IAbonnementKanaalValidator
{
    public bool Validate(AbonnementKanaal abonnementkanaal, Kanaal kanaal, List<ValidationError> errors)
    {
        var kanaalFilterMap = kanaal.Filters.ToHashSet();

        foreach (var filter in abonnementkanaal.Filters)
        {
            if (filter.Key == "#resource")
            {
                continue;
            }
            if (filter.Key == "#actie")
            {
                string[] acties = ["create", "update", "destroy"];

                if (!acties.Contains(filter.Value))
                {
                    errors.Add(
                        new ValidationError(
                            "filter",
                            ErrorCode.NotFound,
                            $"In het abonnement is bij filter '#actie' een incorrecte waarde '{filter.Value}' opgegeven."
                        )
                    );
                }
            }
            else if (!kanaalFilterMap.Contains(filter.Key))
            {
                errors.Add(
                    new ValidationError("filter", ErrorCode.NotFound, $"In het abonnement is een niet bestaand filter '{filter.Key}' opgegeven.")
                );
            }
        }
        return !errors.Any();
    }
}
