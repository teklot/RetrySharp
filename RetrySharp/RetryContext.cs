using System;

namespace RetrySharp;

/// <summary>
/// Context information about a retry attempt.
/// </summary>
/// <param name="attempt">The attempt number (starting at 1).</param>
/// <param name="exception">The exception that occurred during the attempt.</param>
/// <param name="delay">The calculated delay before the next attempt, if any.</param>
public readonly struct RetryContext(int attempt, Exception exception, TimeSpan? delay)
{
    /// <summary>
    /// The attempt number (starting at 1).
    /// </summary>
    public int Attempt { get; } = attempt;

    /// <summary>
    /// The exception that occurred during the attempt.
    /// </summary>
    public Exception Exception { get; } = exception;

    /// <summary>
    /// The calculated delay before the next attempt, if any.
    /// </summary>
    public TimeSpan? Delay { get; } = delay;
}
