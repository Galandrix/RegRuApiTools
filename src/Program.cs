namespace RegRuDns;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        AppSettings.Load();
        Application.Run(new MainForm());
    }
}
