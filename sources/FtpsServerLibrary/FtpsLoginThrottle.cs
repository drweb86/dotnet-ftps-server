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
    private DateTime _nextSweepUtc;

    // Failures are forgotten one lockout window after the last one, so an address that
    // fails a few times and never returns does not keep its entry for the whole uptime.
    private struct Entry
    {
        public int Failures;
        public DateTime LockedUntilUtc;
        public DateTime LastFailureUtc;

        public readonly bool IsStale(DateTime utcNow) =>
            LockedUntilUtc != default ? LockedUntilUtc <= utcNow : LastFailureUtc.Add(Lockout) <= utcNow;
    }

    internal int Count
    {
        get
        {
            lock (_gate)
                return _byIp.Count;
        }
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
            if (entry.IsStale(utcNow))
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
            SweepStale(utcNow);

            _byIp.TryGetValue(ip, out var entry);
            if (entry.LockedUntilUtc > utcNow)
                return true;
            if (entry.IsStale(utcNow))
                entry = default;

            entry.Failures++;
            entry.LastFailureUtc = utcNow;
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

    // Called under _gate. Runs at most once per lockout window, so its cost stays linear
    // in the entries that window can add.
    private void SweepStale(DateTime utcNow)
    {
        if (utcNow < _nextSweepUtc)
            return;
        _nextSweepUtc = utcNow.Add(Lockout);

        List<IPAddress>? stale = null;
        foreach (var (ip, entry) in _byIp)
        {
            if (entry.IsStale(utcNow))
                (stale ??= []).Add(ip);
        }

        if (stale is null)
            return;
        foreach (var ip in stale)
            _byIp.Remove(ip);
    }

    internal static IPAddress Normalize(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }
}
