using System.Text;
using Roxit.ZGW.Common.Contracts.Extensions;

namespace Roxit.ZGW.Common.Contracts;

public interface IQueryParameters { }

public interface IExpandQueryParameter : IQueryParameters
{
    public string Expand { get; set; }
}

public abstract class QueryParameters : IQueryParameters
{
    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var parameter in this.GetParameters())
        {
            var value = parameter.GetValue(this);
            if (value != default)
                sb.Append($"{parameter.ParameterName} = {value}");
        }

        return sb.ToString();
    }
}
