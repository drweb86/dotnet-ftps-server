using System.Collections.Generic;

namespace FtpsServerLibrary;

public class FtpsServerConfiguration
{
    public FtpsServerSettings ServerSettings { get; set; } = new FtpsServerSettings();
    public List<FtpsServerUserAccount> Users { get; set; } = [];
}
