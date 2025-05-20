using System.Text;
using OneGround.ZGW.Common.Contracts.Extensions;

namespace OneGround.ZGW.Common.Contracts;

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
