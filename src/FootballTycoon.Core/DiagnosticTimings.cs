#if ENDURANCE
using System.Diagnostics;

namespace FootballTycoon.Core;

// Diagnostic assembly only. Thread-local counters keep independent runs/tests isolated.
public static class DiagnosticTimings
{
    public sealed record Measurement(long Calls, double Milliseconds);
    [ThreadStatic] private static Dictionary<string, Measurement>? totals;
    public static void Reset() => totals = new(StringComparer.Ordinal);
    public static Dictionary<string, Measurement> Snapshot() => new(totals ?? [], StringComparer.Ordinal);
    public static Scope Measure(string name) => new(name);
    public readonly struct Scope(string name) : IDisposable
    {
        private readonly long started = Stopwatch.GetTimestamp();
        public void Dispose()
        {
            totals ??= new(StringComparer.Ordinal);
            var previous = totals.GetValueOrDefault(name) ?? new(0, 0);
            totals[name] = new(previous.Calls + 1, previous.Milliseconds + Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
#endif
