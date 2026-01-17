using FtpsServerLibrary;
using NLog;

namespace FtpsServerConsole;

public class FileLog: IFtpsServerLog
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public void Debug(string message)
    {
        _logger.Debug(message); 
    }
    
    public void Error(Exception ex, string message)
    {
        _logger.Error(ex, message);
    }

    public void Fatal(Exception ex, string message)
    {
        _logger.Fatal(ex, message);
    }

    public void Info(string message)
    {
        _logger.Info(message);
    }

    public void Warn(string message)
    {
        _logger.Warn(message);
    }
}
