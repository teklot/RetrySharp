using System;

namespace RetrySharp;

/// <summary>
/// Delegate for calculating the delay between retry attempts.
/// </summary>
/// <param name="attempt">The current attempt number (starting at 1).</param>
/// <param name="exception">The exception that triggered the retry.</param>
/// <returns>The delay to wait before the next attempt, or null/Zero for no delay.</returns>
public delegate TimeSpan? DelayStrategy(int attempt, Exception exception);

/// <summary>
/// Delegate for observing retry attempts.
/// </summary>
/// <param name="context">The context containing information about the retry attempt.</param>
public delegate void RetryCallback(RetryContext context);

/// <summary>
/// Delegate for filtering which exceptions should trigger a retry.
/// </summary>
/// <param name="exception">The exception to check.</param>
/// <returns>True if the exception should be retried; false to fail immediately.</returns>
public delegate bool ExceptionFilter(Exception exception);

/// <summary>
/// Configuration options for the retry mechanism.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Default retry options: 3 attempts, no delay, retries all retriable exceptions.
    /// </summary>
    public static readonly RetryOptions Default = new();

    /// <summary>
    /// The maximum number of attempts allowed, including the initial call. Default is 3.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Optional strategy to calculate the delay between retry attempts.
    /// </summary>
    public DelayStrategy? DelayStrategy { get; set; }

    /// <summary>
    /// Optional callback executed before each retry attempt.
    /// </summary>
    public RetryCallback? OnRetry { get; set; }

    /// <summary>
    /// Optional filter to determine if an exception should be retried.
    /// </summary>
    public ExceptionFilter? ExceptionFilter { get; set; }

    internal static bool IsRetriable(Exception exception)
    {
        return exception is not (OperationCanceledException or OutOfMemoryException or StackOverflowException);
    }
}
