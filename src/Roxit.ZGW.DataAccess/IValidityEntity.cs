using System;

namespace Roxit.ZGW.DataAccess;

public interface IValidityEntity
{
    Guid Id { get; set; }
    DateOnly? EindeGeldigheid { get; set; }
}
