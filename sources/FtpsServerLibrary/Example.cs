using System;
using System.Collections.Generic;
using System.Text;

namespace FtpsServerLibrary;

internal class Example
{
    class StubLog : IFtpsServerLog
    {
        public void Debug(string message) { }
        public void Error(Exception ex, string message) { }
        public void Fatal(Exception ex, string message) { }
        public void Info(string message) { }
        public void Warn(string message) { }
    }

    public void StartServer()
    {
        var config = new FtpsServerConfiguration
        {
            ServerSettings = new FtpsServerSettings { },
            Users = [
                new FtpsServerUserAccount { Folder = @"c:\folder", Login = "admin", Password = "admin", Read = true, Write = true}
            ]
        };
        var server = new FtpsServer(new StubLog(), config);
        server.Start();
        Console.ReadLine();
        server.Stop();
    }
}
