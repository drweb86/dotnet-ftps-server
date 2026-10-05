namespace FtpsServerLibrary.Tests;

// Live servers share the machine's TLS session cache and TCP ports.
// Running them together makes a data connection skip session resumption and get rejected.
[CollectionDefinition("FtpsServer", DisableParallelization = true)]
public sealed class FtpsServerCollection;
