using System;

namespace RetrySharp;

public static class RetryDelays
{
    public static DelayStrategy Fixed(TimeSpan delay)
    {
        return (attempt, ex) => delay;
    }

    public static DelayStrategy Linear(TimeSpan initialDelay, TimeSpan factor)
    {
        return (attempt, ex) => TimeSpan.FromTicks(initialDelay.Ticks + (factor.Ticks * (attempt - 1)));
    }

    public static DelayStrategy Exponential(TimeSpan initialDelay, double multiplier = 2.0)
    {
        return (attempt, ex) => TimeSpan.FromTicks((long)(initialDelay.Ticks * Math.Pow(multiplier, attempt - 1)));
    }

    public static DelayStrategy Exponential(TimeSpan initialDelay, TimeSpan maxDelay, double multiplier = 2.0)
    {
        return (attempt, ex) =>
        {
            long ticks = (long)(initialDelay.Ticks * Math.Pow(multiplier, attempt - 1));
            return TimeSpan.FromTicks(Math.Min(ticks, maxDelay.Ticks));
        };
    }

    [ThreadStatic]
    private static Random? _random;

    private static Random GetRandom() => _random ??= new Random();

    public static DelayStrategy WithJitter(DelayStrategy strategy, double jitterFactor = 0.1)
    {
        if (strategy == null) throw new ArgumentNullException(nameof(strategy));
        
        return (attempt, ex) =>
        {
            TimeSpan? delay = strategy(attempt, ex);
            if (!delay.HasValue) return null;

            double jitter = (GetRandom().NextDouble() * 2.0 - 1.0) * jitterFactor;
            return TimeSpan.FromTicks((long)(delay.Value.Ticks * (1.0 + jitter)));
        };
    }
}
