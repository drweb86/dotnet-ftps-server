using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FtpsServerLibrary;

static class FtpsTlsSession
{
    private const uint SessionInfoAttribute = 0x5D;
    private const uint SessionReconnectFlag = 0x1;

    public static bool SameAsControl(SslStream control, SslStream data)
    {
        if (!TryRead(control, out var controlId, out _) || !TryRead(data, out var dataId, out var dataResumed))
        {
            // Android's TLS stack does not expose a session id to this library.
            // Rejecting here would fail every protected transfer. The peer address is still checked.
            return SessionsMatch(false, [], [], false, SslProtocols.None, SslProtocols.None,
                OperatingSystem.IsAndroid(), OperatingSystem.IsWindows());
        }

        if (SessionsMatch(true, controlId, dataId, dataResumed, control.SslProtocol, data.SslProtocol,
            OperatingSystem.IsAndroid(), OperatingSystem.IsWindows()))
            return true;

        // Schannel resumes an OpenSSL TLS 1.2 session with an empty id and the same master
        // secret. TLS 1.3 gets a new id, and after a few transfers Schannel stops resuming
        // altogether, so a TLS 1.3 data connection is accepted the way it is on Windows.
        // The peer address is checked separately. A TLS 1.2 full handshake is still refused.
        if (dataResumed && SameResumedSecret(control, data))
            return true;

        return control.SslProtocol == SslProtocols.Tls13 && data.SslProtocol == SslProtocols.Tls13;
    }

    // acceptUnreadableSession is Android. acceptAnyResumedTls13 is Windows: Schannel gives a
    // resumed TLS 1.3 connection a fresh session id, so only the reconnect flag remains.
    internal static bool SessionsMatch(
        bool sessionReadable,
        byte[] controlId,
        byte[] dataId,
        bool dataResumed,
        SslProtocols controlProtocol,
        SslProtocols dataProtocol,
        bool acceptUnreadableSession,
        bool acceptAnyResumedTls13)
    {
        if (!sessionReadable)
            return acceptUnreadableSession;

        // Schannel resumes one TLS 1.3 ticket only a handful of times, then finishes a full
        // handshake while the client is still on that control connection. A resumed Windows
        // TLS 1.3 session also gets a new id, so the reconnect flag never proved this was the
        // control session. The peer address is checked separately. TLS 1.2 and OpenSSL still
        // have a session id that has to match, and a full handshake there is rejected.
        if (!dataResumed)
        {
            return acceptAnyResumedTls13
                && controlProtocol == SslProtocols.Tls13
                && dataProtocol == SslProtocols.Tls13;
        }

        // Identical session ids identify the same session: TLS 1.2 on every platform,
        // and TLS 1.3 on OpenSSL, which restores the original session from the ticket.
        if (controlId.Length > 0 && dataId.Length > 0 && controlId.AsSpan().SequenceEqual(dataId))
            return true;

        // On Windows no per-session value is left to compare, so accept any session the OS
        // proves this server issued. On other platforms unequal ids mean a different session.
        return controlProtocol == SslProtocols.Tls13
            && dataProtocol == SslProtocols.Tls13
            && acceptAnyResumedTls13;
    }

    private static bool TryRead(SslStream stream, out byte[] sessionId, out bool resumed)
    {
        sessionId = [];
        resumed = false;
        var context = FindContext(stream);
        if (context == null)
            return false;

        if (OperatingSystem.IsWindows())
            return TryReadWindows(context, out sessionId, out resumed);

        return TryReadOpenSsl(context, out sessionId, out resumed);
    }

    private static object? FindContext(SslStream stream)
    {
        for (var type = stream.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField("_securityContext", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(stream);
        }

        return null;
    }

    private static bool TryReadWindows(object context, out byte[] sessionId, out bool resumed)
    {
        sessionId = [];
        resumed = false;
        if (!TryCopyHandle(context, out var handle) || handle.IsZero)
            return false;

        var buffer = Marshal.AllocHGlobal(8 + 32);
        try
        {
            if (QueryContextAttributesW(ref handle, SessionInfoAttribute, buffer) != 0)
                return false;

            var flags = (uint)Marshal.ReadInt32(buffer);
            var length = (uint)Marshal.ReadInt32(buffer, 4);
            resumed = (flags & SessionReconnectFlag) != 0;
            if (length == 0 || length > 32)
                return true;

            sessionId = new byte[length];
            Marshal.Copy(buffer + 8, sessionId, 0, sessionId.Length);
            if (IsAllZero(sessionId))
                sessionId = [];
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool TryCopyHandle(object context, out SecHandle handle)
    {
        handle = default;
        for (var type = context.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField("_handle", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                continue;

            var value = field.GetValue(context);
            if (value == null)
                return false;

            var pointers = new IntPtr[2];
            var found = 0;
            for (var handleType = value.GetType(); handleType != null && found < 2; handleType = handleType.BaseType)
            {
                foreach (var part in handleType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (part.FieldType != typeof(IntPtr) || found >= 2)
                        continue;
                    pointers[found++] = (IntPtr)(part.GetValue(value) ?? IntPtr.Zero);
                }
            }

            if (found < 2)
                return false;

            handle = new SecHandle { dwLower = pointers[0], dwUpper = pointers[1] };
            return true;
        }

        return false;
    }

    private static bool TryReadOpenSsl(object context, out byte[] sessionId, out bool resumed)
    {
        sessionId = [];
        resumed = false;
        if (context is not SafeHandle handle || handle.IsInvalid || handle.IsClosed)
            return false;

        var ssl = handle.DangerousGetHandle();
        if (ssl == IntPtr.Zero || !OpenSsl.IsAvailable)
            return false;

        var reused = false;
        var ok = false;
        handle.DangerousAddRef(ref ok);
        try
        {
            reused = OpenSsl.SessionReused(ssl);
            var session = OpenSsl.GetSession(ssl);
            if (session == IntPtr.Zero)
            {
                resumed = reused;
                return true;
            }

            var idPointer = OpenSsl.SessionId(session, out var length);
            if (idPointer != IntPtr.Zero && length > 0 && length <= 32)
            {
                sessionId = new byte[(int)length];
                Marshal.Copy(idPointer, sessionId, 0, (int)length);
                if (IsAllZero(sessionId))
                    sessionId = [];
            }
        }
        finally
        {
            if (ok)
                handle.DangerousRelease();
        }

        resumed = reused;
        return true;
    }

    private static bool SameResumedSecret(SslStream control, SslStream data)
    {
        if (!OpenSsl.IsAvailable)
            return false;
        if (FindContext(control) is not SafeHandle controlHandle || controlHandle.IsInvalid || controlHandle.IsClosed)
            return false;
        if (FindContext(data) is not SafeHandle dataHandle || dataHandle.IsInvalid || dataHandle.IsClosed)
            return false;

        var controlRef = false;
        var dataRef = false;
        controlHandle.DangerousAddRef(ref controlRef);
        dataHandle.DangerousAddRef(ref dataRef);
        try
        {
            var controlSession = OpenSsl.GetSession(controlHandle.DangerousGetHandle());
            var dataSession = OpenSsl.GetSession(dataHandle.DangerousGetHandle());
            if (controlSession == IntPtr.Zero || dataSession == IntPtr.Zero)
                return false;
            if (controlSession == dataSession)
                return true;

            var controlKey = OpenSsl.MasterKey(controlSession);
            var dataKey = OpenSsl.MasterKey(dataSession);
            if (controlKey.Length > 0 && controlKey.AsSpan().SequenceEqual(dataKey))
                return true;

            var controlTicket = OpenSsl.Ticket(controlSession);
            var dataTicket = OpenSsl.Ticket(dataSession);
            return controlTicket.Length > 0 && controlTicket.AsSpan().SequenceEqual(dataTicket);
        }
        finally
        {
            if (dataRef)
                dataHandle.DangerousRelease();
            if (controlRef)
                controlHandle.DangerousRelease();
        }
    }

    private static bool IsAllZero(byte[] value)
    {
        foreach (var b in value)
        {
            if (b != 0)
                return false;
        }

        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecHandle
    {
        public IntPtr dwLower;
        public IntPtr dwUpper;
        public readonly bool IsZero => dwLower == IntPtr.Zero && dwUpper == IntPtr.Zero;
    }

    [DllImport("secur32.dll", EntryPoint = "QueryContextAttributesW", ExactSpelling = true)]
    private static extern int QueryContextAttributesW(ref SecHandle context, uint attribute, IntPtr buffer);

    private static class OpenSsl
    {
        private static readonly SessionReusedDelegate? SessionReusedImpl;
        private static readonly GetSessionDelegate? GetSessionImpl;
        private static readonly SessionIdDelegate? SessionIdImpl;
        private static readonly MasterKeyDelegate? MasterKeyImpl;
        private static readonly TicketDelegate? TicketImpl;

        static OpenSsl()
        {
            if (OperatingSystem.IsWindows())
                return;

            foreach (var name in new[] { "libssl.so.3", "libssl.so", "libssl.dylib", "ssl", "libSystem.Security.Cryptography.Native.Android.so" })
            {
                if (!NativeLibrary.TryLoad(name, out var library))
                    continue;

                if (!NativeLibrary.TryGetExport(library, "SSL_session_reused", out var reused) ||
                    !NativeLibrary.TryGetExport(library, "SSL_SESSION_get_id", out var sessionId))
                    continue;

                // OpenSSL exports SSL_get_session. SSL_get0_session is only a macro for it.
                if (!NativeLibrary.TryGetExport(library, "SSL_get0_session", out var session) &&
                    !NativeLibrary.TryGetExport(library, "SSL_get_session", out session))
                    continue;

                SessionReusedImpl = Marshal.GetDelegateForFunctionPointer<SessionReusedDelegate>(reused);
                GetSessionImpl = Marshal.GetDelegateForFunctionPointer<GetSessionDelegate>(session);
                SessionIdImpl = Marshal.GetDelegateForFunctionPointer<SessionIdDelegate>(sessionId);
                if (NativeLibrary.TryGetExport(library, "SSL_SESSION_get_master_key", out var masterKey))
                    MasterKeyImpl = Marshal.GetDelegateForFunctionPointer<MasterKeyDelegate>(masterKey);
                if (NativeLibrary.TryGetExport(library, "SSL_SESSION_get0_ticket", out var ticket))
                    TicketImpl = Marshal.GetDelegateForFunctionPointer<TicketDelegate>(ticket);
                return;
            }
        }

        public static bool IsAvailable => SessionReusedImpl != null;

        public static bool SessionReused(IntPtr ssl) => SessionReusedImpl!(ssl) != 0;

        public static IntPtr GetSession(IntPtr ssl) => GetSessionImpl!(ssl);

        public static byte[] MasterKey(IntPtr session)
        {
            if (MasterKeyImpl == null || session == IntPtr.Zero)
                return [];

            var buffer = Marshal.AllocHGlobal(64);
            try
            {
                var length = MasterKeyImpl(session, buffer, 64);
                if (length == 0 || length > 64)
                    return [];

                var key = new byte[(int)length];
                Marshal.Copy(buffer, key, 0, key.Length);
                return key;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public static byte[] Ticket(IntPtr session)
        {
            if (TicketImpl == null || session == IntPtr.Zero)
                return [];

            TicketImpl(session, out var pointer, out var length);
            if (pointer == IntPtr.Zero || length == 0 || length > 8192)
                return [];

            var ticket = new byte[(int)length];
            Marshal.Copy(pointer, ticket, 0, ticket.Length);
            return ticket;
        }

        public static IntPtr SessionId(IntPtr session, out uint length) => SessionIdImpl!(session, out length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SessionReusedDelegate(IntPtr ssl);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetSessionDelegate(IntPtr ssl);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate nuint MasterKeyDelegate(IntPtr session, IntPtr output, nuint outputLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void TicketDelegate(IntPtr session, out IntPtr ticket, out nuint length);

        // OpenSSL writes an unsigned int, which is 4 bytes on the platforms this runs on.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr SessionIdDelegate(IntPtr session, out uint length);
    }
}
