using System;

namespace OneGround.ZGW.DataAccess;

public interface IAuditableEntity : IBaseEntity
{
    DateTime CreationTime { get; set; }
    DateTime? ModificationTime { get; set; }
    string CreatedBy { get; set; }
    string ModifiedBy { get; set; }
}
