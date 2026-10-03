using System;
using System.Collections.Generic;
using System.Net;

namespace FtpsServerLibrary;

class FtpsLoginThrottle
{
    internal const int FailureLimit = 5;
    internal static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan Lockout = TimeSpan.FromSeconds(60);

    private readonly object _gate = new();
    private readonly Dictionary<IPAddress, Entry> _byIp = new();

    private struct Entry
    {
        public int Failures;
        public DateTime LockedUntilUtc;
    }

    public bool IsLocked(IPAddress address, DateTime utcNow)
    {
        var ip = Normalize(address);
        lock (_gate)
        {
            if (!_byIp.TryGetValue(ip, out var entry))
                return false;
            if (entry.LockedUntilUtc > utcNow)
                return true;
            if (entry.LockedUntilUtc != default)
                _byIp.Remove(ip);
            return false;
        }
    }

    public void RegisterSuccess(IPAddress address)
    {
        lock (_gate)
        {
            _byIp.Remove(Normalize(address));
        }
    }

    public bool RegisterFailure(IPAddress address, DateTime utcNow)
    {
        var ip = Normalize(address);
        lock (_gate)
        {
            _byIp.TryGetValue(ip, out var entry);
            if (entry.LockedUntilUtc > utcNow)
                return true;
            if (entry.LockedUntilUtc != default)
                entry = default;

            entry.Failures++;
            if (entry.Failures >= FailureLimit)
            {
                entry.LockedUntilUtc = utcNow.Add(Lockout);
                _byIp[ip] = entry;
                return true;
            }

            entry.LockedUntilUtc = default;
            _byIp[ip] = entry;
            return false;
        }
    }

    internal static IPAddress Normalize(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }
}
