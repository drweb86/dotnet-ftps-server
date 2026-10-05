namespace FtpsServerLibrary.Tests;

// Two tests that both probe a free port and then bind it can take the same port.
// The probe and the server start stay in this gate so the port is taken before the next test looks.
static class FtpsTestGate
{
    public static readonly SemaphoreSlim Start = new(1, 1);
}
