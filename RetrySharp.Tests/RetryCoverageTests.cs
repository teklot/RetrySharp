using System;
using System.Threading;
using System.Threading.Tasks;

namespace RetrySharp.Tests;

/// <summary>
/// Branch-coverage tests for the retry logic in Retry.cs, including the edge
/// cases required for 100% branch coverage:
///   - OnRetry callback throwing must not interrupt the retry flow.
///   - MaxAttempts set exactly to 1 (fast path and forced-internal path).
///   - Cancellation triggered exactly between attempts.
/// </summary>
public class RetryCoverageTests
{
    private sealed class Counter
    {
        public int Value;
    }

    // ===== Null argument validation (all public entry points) =====

    [Fact]
    public void Execute_NullAction_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => Retry.Execute(null!));
    }

    [Fact]
    public void Execute_State_NullAction_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => Retry.Execute(0, (Action<int>)null!));
    }

    [Fact]
    public void Execute_Generic_NullFunc_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => Retry.Execute<int>((Func<int>)null!));
    }

    [Fact]
    public void Execute_GenericState_NullFunc_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => Retry.Execute<int, int>(0, (Func<int, int>)null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullFunc_ThrowsArgumentNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Retry.ExecuteAsync((Func<CancellationToken, Task>)null!));
    }

    [Fact]
    public async Task ExecuteAsync_State_NullFunc_ThrowsArgumentNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Retry.ExecuteAsync(0, (Func<int, CancellationToken, Task>)null!));
    }

    [Fact]
    public async Task ExecuteAsync_Generic_NullFunc_ThrowsArgumentNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Retry.ExecuteAsync<int>((Func<CancellationToken, Task<int>>)null!));
    }

    [Fact]
    public async Task ExecuteAsync_GenericState_NullFunc_ThrowsArgumentNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Retry.ExecuteAsync<int, int>(0, (Func<int, CancellationToken, Task<int>>)null!));
    }

    // ===== Default options (null options: covers the options ??= branch) =====

    [Fact]
    public void Execute_State_DefaultOptions()
    {
        var counter = new Counter();
        Retry.Execute(counter, static c => { c.Value++; });
        Assert.Equal(1, counter.Value);
    }

    [Fact]
    public void Execute_Generic_DefaultOptions()
    {
        Assert.Equal(42, Retry.Execute(() => 42));
    }

    [Fact]
    public void Execute_GenericState_DefaultOptions()
    {
        var counter = new Counter();
        int result = Retry.Execute(counter, static c => { c.Value++; return c.Value; });
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_State_DefaultOptions()
    {
        var counter = new Counter();
        await Retry.ExecuteAsync(counter, (c, _) => { c.Value++; return Task.CompletedTask; });
        Assert.Equal(1, counter.Value);
    }

    [Fact]
    public async Task ExecuteAsync_Generic_DefaultOptions()
    {
        Assert.Equal(42, await Retry.ExecuteAsync(_ => Task.FromResult(42)));
    }

    [Fact]
    public async Task ExecuteAsync_GenericState_DefaultOptions()
    {
        var counter = new Counter();
        int result = await Retry.ExecuteAsync(counter, (c, _) => { c.Value++; return Task.FromResult(c.Value); });
        Assert.Equal(1, result);
    }

    // ===== MaxAttempts = 1: fast path (covers IsFastPath true branch) =====

    [Fact]
    public void Execute_MaxAttemptsOne_FastPath_NoRetry()
    {
        int calls = 0;
        Assert.Throws<Exception>(() =>
            Retry.Execute(() => { calls++; throw new Exception("fail"); },
                new RetryOptions { MaxAttempts = 1 }));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Execute_MaxAttemptsOne_FastPath_Succeeds()
    {
        int calls = 0;
        Retry.Execute(() => { calls++; }, new RetryOptions { MaxAttempts = 1 });
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Execute_State_MaxAttemptsOne_FastPath()
    {
        var counter = new Counter();
        Retry.Execute(counter, static c => { c.Value++; }, new RetryOptions { MaxAttempts = 1 });
        Assert.Equal(1, counter.Value);
    }

    [Fact]
    public void Execute_Generic_MaxAttemptsOne_FastPath()
    {
        Assert.Equal(42, Retry.Execute(() => 42, new RetryOptions { MaxAttempts = 1 }));
    }

    [Fact]
    public void Execute_GenericState_MaxAttemptsOne_FastPath()
    {
        var counter = new Counter();
        int result = Retry.Execute(counter, static c => { c.Value++; return c.Value; },
            new RetryOptions { MaxAttempts = 1 });
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_MaxAttemptsOne_FastPath()
    {
        int calls = 0;
        await Retry.ExecuteAsync(async ct => { calls++; await Task.Yield(); },
            new RetryOptions { MaxAttempts = 1 });
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ExecuteAsync_State_MaxAttemptsOne_FastPath()
    {
        var counter = new Counter();
        await Retry.ExecuteAsync(counter, (c, _) => { c.Value++; return Task.CompletedTask; },
            new RetryOptions { MaxAttempts = 1 });
        Assert.Equal(1, counter.Value);
    }

    [Fact]
    public async Task ExecuteAsync_Generic_MaxAttemptsOne_FastPath()
    {
        Assert.Equal(42, await Retry.ExecuteAsync(_ => Task.FromResult(42),
            new RetryOptions { MaxAttempts = 1 }));
    }

    [Fact]
    public async Task ExecuteAsync_GenericState_MaxAttemptsOne_FastPath()
    {
        var counter = new Counter();
        int result = await Retry.ExecuteAsync(counter, (c, _) => { c.Value++; return Task.FromResult(c.Value); },
            new RetryOptions { MaxAttempts = 1 });
        Assert.Equal(1, result);
    }

    // ===== MaxAttempts = 1 with a non-null option forces the internal path (no retry) =====

    [Fact]
    public void Execute_MaxAttemptsOne_WithDelayStrategy_NoRetry()
    {
        int calls = 0;
        Assert.Throws<Exception>(() =>
            Retry.Execute(() => { calls++; throw new Exception("fail"); },
                new RetryOptions { MaxAttempts = 1, DelayStrategy = RetryDelays.Fixed(TimeSpan.FromMilliseconds(1)) }));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Execute_MaxAttemptsOne_WithExceptionFilter_NoRetry()
    {
        int calls = 0;
        Assert.Throws<Exception>(() =>
            Retry.Execute(() => { calls++; throw new Exception("fail"); },
                new RetryOptions { MaxAttempts = 1, ExceptionFilter = _ => true }));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Execute_MaxAttemptsOne_WithOnRetry_NoRetry()
    {
        int calls = 0;
        Assert.Throws<Exception>(() =>
            Retry.Execute(() => { calls++; throw new Exception("fail"); },
                new RetryOptions { MaxAttempts = 1, OnRetry = _ => { } }));
        Assert.Equal(1, calls);
    }

    // ===== Retry flow through the internal path for all shapes =====

    [Fact]
    public void Execute_State_RetriesThenSucceeds()
    {
        var counter = new Counter();
        Retry.Execute(counter, static c =>
        {
            c.Value++;
            if (c.Value < 3) throw new Exception("fail");
        }, new RetryOptions { MaxAttempts = 3 });
        Assert.Equal(3, counter.Value);
    }

    [Fact]
    public void Execute_Generic_RetriesThenReturns()
    {
        int calls = 0;
        int result = Retry.Execute(() =>
        {
            calls++;
            if (calls < 3) throw new Exception("fail");
            return 42;
        }, new RetryOptions { MaxAttempts = 3 });
        Assert.Equal(42, result);
        Assert.Equal(3, calls);
    }

    [Fact]
    public void Execute_GenericState_RetriesThenReturns()
    {
        var counter = new Counter();
        int result = Retry.Execute(counter, static c =>
        {
            c.Value++;
            if (c.Value < 3) throw new Exception("fail");
            return 7;
        }, new RetryOptions { MaxAttempts = 3 });
        Assert.Equal(7, result);
        Assert.Equal(3, counter.Value);
    }

    [Fact]
    public async Task ExecuteAsync_State_RetriesThenSucceeds()
    {
        var counter = new Counter();
        await Retry.ExecuteAsync(counter, async (c, _) =>
        {
            c.Value++;
            await Task.Yield();
            if (c.Value < 3) throw new Exception("fail");
        }, new RetryOptions { MaxAttempts = 3 });
        Assert.Equal(3, counter.Value);
    }

    [Fact]
    public async Task ExecuteAsync_Generic_RetriesThenReturns()
    {
        int calls = 0;
        int result = await Retry.ExecuteAsync(async ct =>
        {
            calls++;
            await Task.Yield();
            if (calls < 3) throw new Exception("fail");
            return 5;
        }, new RetryOptions { MaxAttempts = 3 });
        Assert.Equal(5, result);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ExecuteAsync_GenericState_RetriesThenReturns()
    {
        var counter = new Counter();
        int result = await Retry.ExecuteAsync(counter, async (c, _) =>
        {
            c.Value++;
            await Task.Yield();
            if (c.Value < 3) throw new Exception("fail");
            return 9;
        }, new RetryOptions { MaxAttempts = 3 });
        Assert.Equal(9, result);
        Assert.Equal(3, counter.Value);
    }

    // ===== Edge case: OnRetry throwing must not interrupt the flow =====

    [Fact]
    public void Execute_OnRetryThrows_DoesNotInterruptFlow()
    {
        int calls = 0;
        Retry.Execute(() =>
        {
            calls++;
            if (calls < 3) throw new Exception("fail");
        }, new RetryOptions
        {
            MaxAttempts = 3,
            OnRetry = _ => throw new InvalidOperationException("OnRetry crashed")
        });
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ExecuteAsync_OnRetryThrows_DoesNotInterruptFlow()
    {
        int calls = 0;
        await Retry.ExecuteAsync(async ct =>
        {
            calls++;
            await Task.Yield();
            if (calls < 3) throw new Exception("fail");
        }, new RetryOptions
        {
            MaxAttempts = 3,
            OnRetry = _ => throw new InvalidOperationException("OnRetry crashed")
        });
        Assert.Equal(3, calls);
    }

    // ===== Edge case: cancellation triggered exactly between attempts =====

    [Fact]
    public async Task ExecuteAsync_CancellationBetweenAttempts_Throws()
    {
        using var cts = new CancellationTokenSource();
        int calls = 0;

        var ex = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            Retry.ExecuteAsync(async ct =>
            {
                calls++;
                await Task.Yield();
                throw new Exception("fail");
            }, new RetryOptions
            {
                MaxAttempts = 3,
                DelayStrategy = RetryDelays.Fixed(TimeSpan.FromMinutes(1)),
                OnRetry = _ => cts.Cancel()
            }, cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);

        Assert.Equal(1, calls);
    }

    // ===== Delay handling: positive and clamped delays =====

    [Fact]
    public void Execute_PositiveDelay_SleepsBetweenAttempts()
    {
        int calls = 0;
        Retry.Execute(() =>
        {
            calls++;
            if (calls < 3) throw new Exception("fail");
        }, new RetryOptions
        {
            MaxAttempts = 3,
            DelayStrategy = RetryDelays.Fixed(TimeSpan.FromMilliseconds(1))
        });
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ExecuteAsync_PositiveDelay_WaitsBetweenAttempts()
    {
        int calls = 0;
        await Retry.ExecuteAsync(async ct =>
        {
            calls++;
            await Task.Yield();
            if (calls < 3) throw new Exception("fail");
        }, new RetryOptions
        {
            MaxAttempts = 3,
            DelayStrategy = RetryDelays.Fixed(TimeSpan.FromMilliseconds(1))
        });
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ExecuteAsync_NegativeDelay_ClampedToZero_NoWait()
    {
        int calls = 0;
        await Retry.ExecuteAsync(async ct =>
        {
            calls++;
            await Task.Yield();
            if (calls < 3) throw new Exception("fail");
        }, new RetryOptions
        {
            MaxAttempts = 3,
            DelayStrategy = (_, _) => TimeSpan.FromMilliseconds(-50)
        });
        Assert.Equal(3, calls);
    }

    // ===== Exception filter returning true retries =====

    [Fact]
    public void Execute_FilterTrue_RetriesThenSucceeds()
    {
        int calls = 0;
        Retry.Execute(() =>
        {
            calls++;
            if (calls < 3) throw new Exception("fail");
        }, new RetryOptions
        {
            MaxAttempts = 3,
            ExceptionFilter = ex => ex is Exception
        });
        Assert.Equal(3, calls);
    }
}
