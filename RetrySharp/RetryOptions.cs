using System;

namespace RetrySharp;

public delegate TimeSpan? DelayStrategy(int attempt, Exception exception);
public delegate void RetryCallback(RetryContext context);
public delegate bool ExceptionFilter(Exception exception);

public sealed class RetryOptions
{
    public static readonly RetryOptions Default = new();

    public int MaxAttempts { get; set; } = 3;
    public DelayStrategy? DelayStrategy { get; set; }
    public RetryCallback? OnRetry { get; set; }
    public ExceptionFilter? ExceptionFilter { get; set; }

    internal static bool IsRetriable(Exception exception)
    {
        return exception is not (OperationCanceledException or OutOfMemoryException or StackOverflowException);
    }
}
