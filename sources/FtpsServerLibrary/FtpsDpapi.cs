using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace FtpsServerLibrary;

/// <summary>
/// Current-user DPAPI via crypt32. Same scope as DataProtectionScope.CurrentUser with no entropy.
/// </summary>
static class FtpsDpapi
{
    private const uint UiForbidden = 0x1;

    public static byte[] Protect(byte[] plainBytes) => Invoke(plainBytes, protect: true);

    public static byte[] Unprotect(byte[] protectedBytes) => Invoke(protectedBytes, protect: false);

    private static byte[] Invoke(byte[] input, bool protect)
    {
        var inputPtr = Marshal.AllocHGlobal(input.Length);
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);
            var inputBlob = new DataBlob { cbData = (uint)input.Length, pbData = inputPtr };
            var outputBlob = new DataBlob();
            var ok = protect
                ? CryptProtectData(ref inputBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbidden, ref outputBlob)
                : CryptUnprotectData(ref inputBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbidden, ref outputBlob);
            if (!ok)
                throw new CryptographicException(Marshal.GetLastWin32Error());

            try
            {
                var result = new byte[outputBlob.cbData];
                if (outputBlob.cbData > 0)
                    Marshal.Copy(outputBlob.pbData, result, 0, (int)outputBlob.cbData);
                return result;
            }
            finally
            {
                if (outputBlob.pbData != IntPtr.Zero)
                    LocalFree(outputBlob.pbData);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public uint cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(
        ref DataBlob pDataIn,
        string? szDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        ref DataBlob pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(
        ref DataBlob pDataIn,
        IntPtr ppszDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        ref DataBlob pDataOut);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
