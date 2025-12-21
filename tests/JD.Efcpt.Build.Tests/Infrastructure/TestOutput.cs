using System.Text;

namespace JD.Efcpt.Build.Tests.Infrastructure;

internal static class TestOutput
{
    public static string DescribeErrors(TestBuildEngine engine)
    {
        var sb = new StringBuilder();
        foreach (var e in engine.Errors)
        {
            sb.AppendLine($"{e.Code}: {e.Message}");
        }
        return sb.ToString();
    }
}
