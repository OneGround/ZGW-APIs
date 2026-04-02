using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OneGround.ZGW.DataAccess.Encryption;

public sealed class DataProtectionConverter : ValueConverter<string, string>
{
    public DataProtectionConverter(IDatabaseProtector databaseProtector)
        : base(x => databaseProtector.Protect(x), x => databaseProtector.Unprotect(x))
    {
        ArgumentNullException.ThrowIfNull(databaseProtector);
    }
}
