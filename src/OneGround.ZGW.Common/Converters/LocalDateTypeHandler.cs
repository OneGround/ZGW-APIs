using System;
using System.Data;
using Dapper;

namespace OneGround.ZGW.Common.Converters;

public class LocalDateTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override DateTime Parse(object value)
    {
        if (value is NodaTime.Instant instant)
        {
            return instant.ToDateTimeUtc();
        }

        throw new DataException($"Unable to convert {value} to LocalDate");
    }

    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value;
    }
}
