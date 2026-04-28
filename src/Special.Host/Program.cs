using System.Windows.Forms;

namespace Special.Host;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var runtime = HostRuntime.CreateDefault();
        runtime.Run();
    }
}
