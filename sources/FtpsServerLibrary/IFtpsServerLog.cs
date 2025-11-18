
namespace FtpsServerLibrary;

public interface IFtpsServerLog
{
    void Debug(string message);
    void Error(Exception ex, string message);
    void Fatal(Exception ex, string message);
    void Info(string message);
    void Warn(string message);
}
