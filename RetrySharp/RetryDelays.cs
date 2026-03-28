using System;

namespace RetrySharp;

/// <summary>
/// Provides built-in delay strategies for the retry mechanism.
/// </summary>
public static class RetryDelays
{
    /// <summary>
    /// Returns a strategy with a fixed delay between attempts.
    /// </summary>
    /// <param name="delay">The fixed delay to use.</param>
    /// <returns>A delay strategy delegate.</returns>
    public static DelayStrategy Fixed(TimeSpan delay)
    {
        return (attempt, ex) => delay;
    }

    /// <summary>
    /// Returns a strategy that increases the delay linearly with each attempt.
    /// </summary>
    /// <param name="initialDelay">The delay for the first retry.</param>
    /// <param name="factor">The amount to add to the delay for each subsequent attempt.</param>
    /// <returns>A delay strategy delegate.</returns>
    public static DelayStrategy Linear(TimeSpan initialDelay, TimeSpan factor)
    {
        return (attempt, ex) => TimeSpan.FromTicks(initialDelay.Ticks + (factor.Ticks * (attempt - 1)));
    }

    /// <summary>
    /// Returns a strategy that increases the delay exponentially with each attempt.
    /// </summary>
    /// <param name="initialDelay">The delay for the first retry.</param>
    /// <param name="multiplier">The multiplier to apply to the delay for each subsequent attempt. Default is 2.0.</param>
    /// <returns>A delay strategy delegate.</returns>
    public static DelayStrategy Exponential(TimeSpan initialDelay, double multiplier = 2.0)
    {
        return (attempt, ex) => TimeSpan.FromTicks((long)(initialDelay.Ticks * Math.Pow(multiplier, attempt - 1)));
    }

    /// <summary>
    /// Returns a strategy that increases the delay exponentially with each attempt, up to a maximum cap.
    /// </summary>
    /// <param name="initialDelay">The delay for the first retry.</param>
    /// <param name="maxDelay">The maximum allowed delay.</param>
    /// <param name="multiplier">The multiplier to apply to the delay for each subsequent attempt. Default is 2.0.</param>
    /// <returns>A delay strategy delegate.</returns>
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

    /// <summary>
    /// Wraps an existing delay strategy with a jitter factor to randomize the delay.
    /// </summary>
    /// <param name="strategy">The base delay strategy to wrap.</param>
    /// <param name="jitterFactor">The maximum percentage to jitter the delay (e.g., 0.1 for +/- 10%). Default is 0.1.</param>
    /// <returns>A delay strategy delegate with jitter applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when strategy is null.</exception>
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
