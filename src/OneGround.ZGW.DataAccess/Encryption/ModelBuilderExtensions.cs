using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace OneGround.ZGW.DataAccess.Encryption;

public static class ModelBuilderExtensions
{
    public static void ApplyDataProtectionConverters(this ModelBuilder modelBuilder, IDatabaseProtector databaseProtector)
    {
        foreach (
            var property in modelBuilder
                .Model.GetEntityTypes()
                .SelectMany(t =>
                    t.GetProperties()
                        .Where(p => p.PropertyInfo != null && p.PropertyInfo.GetCustomAttributes(typeof(ProtectedDataAttribute), false).Length != 0)
                )
        )
        {
            property.SetPropertyAccessMode(PropertyAccessMode.Field);
            property.SetValueConverter(new DataProtectionConverter(databaseProtector));
        }
    }
}
