using System;
using System.Collections.Generic;

namespace CU.RemoteConsole.Security;

public sealed class RateLimiter
{
    private readonly object gate = new object();
    private readonly Dictionary<string, Window> windows = new Dictionary<string, Window>();
    private readonly int maxPerSecond;

    public RateLimiter(int maxPerSecond)
    {
        this.maxPerSecond = Math.Max(1, maxPerSecond);
    }

    public bool TryAcquire(string key)
    {
        var now = DateTimeOffset.UtcNow;
        lock (gate)
        {
            if (!windows.TryGetValue(key, out var window) || now - window.Start >= TimeSpan.FromSeconds(1))
            {
                windows[key] = new Window(now, 1);
                return true;
            }

            if (window.Count >= maxPerSecond)
            {
                return false;
            }

            window.Count++;
            return true;
        }
    }

    private sealed class Window
    {
        public Window(DateTimeOffset start, int count)
        {
            Start = start;
            Count = count;
        }

        public DateTimeOffset Start { get; }

        public int Count { get; set; }
    }
}
