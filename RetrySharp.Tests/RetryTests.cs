using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RetrySharp.Tests;

public class RetryTests
{
    [Fact]
    public void Execute_SucceedsOnFirstAttempt()
    {
        int calls = 0;
        Retry.Execute(() => { calls++; });
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Execute_SucceedsAfterRetries()
    {
        int calls = 0;
        Retry.Execute(() =>
        {
            calls++;
            if (calls < 3) throw new Exception("fail");
        }, new RetryOptions { MaxAttempts = 3 });
        Assert.Equal(3, calls);
    }

    [Fact]
    public void Execute_ThrowsLastExceptionAfterMaxAttempts()
    {
        int calls = 0;
        var ex = Assert.Throws<Exception>(() =>
            Retry.Execute(() =>
            {
                calls++;
                throw new Exception("fail " + calls);
            }, new RetryOptions { MaxAttempts = 3 }));

        Assert.Equal("fail 3", ex.Message);
        Assert.Equal(3, calls);
    }

    [Fact]
    public void Execute_RespectsExceptionFilter()
    {
        int calls = 0;
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Retry.Execute(() =>
            {
                calls++;
                throw new InvalidOperationException("fail");
            }, new RetryOptions
            {
                MaxAttempts = 3,
                ExceptionFilter = e => e is ArgumentException
            }));

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Execute_CallsOnRetry()
    {
        int retryCalls = 0;
        Assert.Throws<Exception>(() =>
            Retry.Execute(() =>
            {
                throw new Exception("fail");
            }, new RetryOptions
            {
                MaxAttempts = 2,
                OnRetry = ctx =>
                {
                    retryCalls++;
                    Assert.Equal(1, ctx.Attempt);
                    Assert.IsType<Exception>(ctx.Exception);
                }
            }));
        Assert.Equal(1, retryCalls);
    }

    [Fact]
    public void DelayStrategy_FixedWorks()
    {
        var strategy = RetryDelays.Fixed(TimeSpan.FromMilliseconds(50));
        var delay = strategy(1, new Exception());
        Assert.Equal(TimeSpan.FromMilliseconds(50), delay);
    }

    [Fact]
    public void DelayStrategy_LinearWorks()
    {
        var strategy = RetryDelays.Linear(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(10));
        Assert.Equal(TimeSpan.FromMilliseconds(50), strategy(1, new Exception()));
        Assert.Equal(TimeSpan.FromMilliseconds(60), strategy(2, new Exception()));
        Assert.Equal(TimeSpan.FromMilliseconds(70), strategy(3, new Exception()));
    }

    [Fact]
    public void DelayStrategy_ExponentialWorks()
    {
        var strategy = RetryDelays.Exponential(TimeSpan.FromMilliseconds(10), 2.0);
        Assert.Equal(TimeSpan.FromMilliseconds(10), strategy(1, new Exception()));
        Assert.Equal(TimeSpan.FromMilliseconds(20), strategy(2, new Exception()));
        Assert.Equal(TimeSpan.FromMilliseconds(40), strategy(3, new Exception()));
    }

    [Fact]
    public void Execute_DoesNotRetryOnNonRetriableExceptions()
    {
        int calls = 0;
        Assert.Throws<OperationCanceledException>(() =>
            Retry.Execute(() =>
            {
                calls++;
                throw new OperationCanceledException();
            }, new RetryOptions { MaxAttempts = 3 }));
        
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ExecuteAsync_SucceedsAfterRetries()
    {
        int calls = 0;
        await Retry.ExecuteAsync(async ct =>
        {
            calls++;
            await Task.Yield();
            if (calls < 3) throw new Exception("fail");
        }, new RetryOptions { MaxAttempts = 3 });
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ExecuteAsync_RespectsCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int calls = 0;
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Retry.ExecuteAsync(async ct =>
            {
                calls++;
                await Task.Yield();
            }, cancellationToken: cts.Token));

        Assert.Equal(0, calls);
    }
}
