using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace OneGround.ZGW.DataAccess;

public interface IUpsertSeeder
{
    public void Upsert<T>(DbSet<T> dbEntities, IEnumerable<T> seedEntities)
        where T : class, IBaseEntity;
}
