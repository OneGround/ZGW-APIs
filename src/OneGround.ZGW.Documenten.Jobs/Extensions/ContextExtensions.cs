using Hangfire.Console;
using Hangfire.Server;

namespace OneGround.ZGW.Documenten.Jobs.Extensions;

public static class ContextExtensions
{
    public static void WriteLineColored(this PerformContext context, ConsoleTextColor color, string message)
    {
        if (context != null)
        {
            context.SetTextColor(color);
            context.WriteLine(message);
            context.ResetTextColor();
        }
    }
}
