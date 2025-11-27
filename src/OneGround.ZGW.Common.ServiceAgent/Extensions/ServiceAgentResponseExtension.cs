using System.Linq;

namespace OneGround.ZGW.Common.ServiceAgent.Extensions;

public static class ServiceAgentResponseExtension
{
    public static string GetErrorsFromResponse(this ServiceAgentResponse response)
    {
        if (response.Error.InvalidParams != null && response.Error.InvalidParams.Any())
        {
            return string.Join(", ", response.Error.InvalidParams.Select(ip => ip.Reason));
        }
        else if (!string.IsNullOrEmpty(response.Error.Title))
        {
            return response.Error.Title;
        }
        else if (response.Error.Status > 0)
        {
            return "Http Status Code: " + response.Error.Status.ToString();
        }
        else
        {
            return "Unknown.";
        }
    }
}
