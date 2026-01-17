using FtpsServerLibrary;

namespace FtpsServerConsole;

// on Android we don't have possibility to see logs folder
public class StubLog : IFtpsServerLog
{
    public void Debug(string message)
    {
    }

    public void Error(Exception ex, string message)
    {
    }

    public void Fatal(Exception ex, string message)
    {
    }

    public void Info(string message)
    {
    }

    public void Warn(string message)
    {
    }
}
