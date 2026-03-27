using System;

namespace RetrySharp;

public readonly struct RetryContext(int attempt, Exception exception, TimeSpan? delay)
{
    public int Attempt { get; } = attempt;
    public Exception Exception { get; } = exception;
    public TimeSpan? Delay { get; } = delay;
}
