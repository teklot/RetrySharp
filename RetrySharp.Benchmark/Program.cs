using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Polly;
using Polly.Retry;
using RetrySharp;

namespace RetrySharp.Benchmark;

[MemoryDiagnoser]
public class RetryBenchmarks
{
    private static readonly RetryOptions RetrySharpOptions_1 = new() { MaxAttempts = 1 };
    private static readonly RetryOptions RetrySharpOptions_2 = new() { MaxAttempts = 2 };
    
    private static readonly ResiliencePipeline PollyPipeline_FastPath = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 1 }) // Won't be triggered
        .Build();

    private static readonly ResiliencePipeline PollyPipeline_OneRetry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions 
        { 
            MaxRetryAttempts = 1,
            Delay = TimeSpan.Zero,
            BackoffType = DelayBackoffType.Constant
        })
        .Build();

    private int _count;

    [IterationSetup]
    public void Setup() => _count = 0;

    // --- Synchronous Benchmarks ---

    [Benchmark(Baseline = true)]
    public void DirectCall_Sync()
    {
        Work();
    }

    [Benchmark]
    public void RetrySharp_Sync_FastPath_AllocationFree()
    {
        // Using the TState overload to avoid closure allocation
        Retry.Execute(this, static @this => @this.Work(), RetrySharpOptions_1);
    }

    [Benchmark]
    public void RetrySharp_Sync_OneRetry()
    {
        _count = 0;
        Retry.Execute(this, static @this =>
        {
            if (@this._count++ == 0) throw new Exception();
        }, RetrySharpOptions_2);
    }

    [Benchmark]
    public void Polly_Sync_FastPath()
    {
        PollyPipeline_FastPath.Execute(Work);
    }

    [Benchmark]
    public void Polly_Sync_OneRetry()
    {
        _count = 0;
        PollyPipeline_OneRetry.Execute(() =>
        {
            if (_count++ == 0) throw new Exception();
        });
    }

    // --- Asynchronous Benchmarks ---

    [Benchmark]
    public async Task DirectCall_Async()
    {
        await WorkAsync(CancellationToken.None);
    }

    [Benchmark]
    public async Task RetrySharp_Async_FastPath_AllocationFree()
    {
        // Using the TState overload to avoid closure allocation
        await Retry.ExecuteAsync(this, static async (@this, ct) => await @this.WorkAsync(ct), RetrySharpOptions_1);
    }

    [Benchmark]
    public async Task RetrySharp_Async_OneRetry()
    {
        _count = 0;
        await Retry.ExecuteAsync(this, static async (@this, ct) =>
        {
            if (@this._count++ == 0) throw new Exception();
            await Task.Yield();
        }, RetrySharpOptions_2);
    }

    [Benchmark]
    public async Task Polly_Async_FastPath()
    {
        await PollyPipeline_FastPath.ExecuteAsync(async ct => await WorkAsync(ct), CancellationToken.None);
    }

    [Benchmark]
    public async Task Polly_Async_OneRetry()
    {
        _count = 0;
        await PollyPipeline_OneRetry.ExecuteAsync(async ct =>
        {
            if (_count++ == 0) throw new Exception();
            await Task.Yield();
        }, CancellationToken.None);
    }

    private void Work() { /* Simple work */ }
    private Task WorkAsync(CancellationToken ct) => Task.CompletedTask;
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<RetryBenchmarks>();
    }
}
