using System;

namespace OneGround.ZGW.DataAccess;

public interface IValidityEntity
{
    Guid Id { get; set; }
    DateOnly? EindeGeldigheid { get; set; }
}
