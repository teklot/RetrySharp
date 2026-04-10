# RetrySharp

**RetrySharp** is a lightweight, high-performance retry utility for C# with zero external dependencies. Designed for systems where every microsecond and allocation matters.

## Key Features

*   **Ultra-Low Latency:** Optimized for high-throughput and low-latency environments.
*   **Zero Allocations:** Optimized state-based overloads to eliminate closure allocations on hot paths.
*   **Fast-Path Optimization:** Executing with `MaxAttempts = 1` results in a direct call with negligible overhead (~1.0 us).
*   **Sync & Async:** Native, first-class support for both execution paths.
*   **Predictable:** No hidden behavior or complex policy chaining.
*   **Minimalist:** Tiny API surface (learnable in minutes).

## Installation

```bash
dotnet add package RetrySharp
```

## Usage

### 1. High Performance (Zero Allocation)
Use the `TState` overloads to pass data into the action without creating a closure (eliminates heap allocations).

```csharp
// Passes 'this' or any other state object directly to the static callback
Retry.Execute(state, static s => s.DoWork());
```

### 2. Basic Retry
Retries up to 3 times (default) if any exception occurs.

```csharp
Retry.Execute(() => DoWork());
```

### 3. Async Retry with Cancellation
Supports `CancellationToken` and `ConfigureAwait(false)` internally.

```csharp
await Retry.ExecuteAsync(async ct => 
{
    await DoDownloadAsync(ct);
}, cancellationToken: cts.Token);
```

### 4. Exponential Backoff
Includes built-in strategies for fixed, linear, and exponential delays.

```csharp
var options = new RetryOptions
{
    MaxAttempts = 5,
    DelayStrategy = RetryDelays.Exponential(
        initialDelay: TimeSpan.FromMilliseconds(100),
        maxDelay: TimeSpan.FromSeconds(2)
    )
};

Retry.Execute(() => API.Call(), options);
```

### 5. Exception Filtering
Only retry for specific exception types.

```csharp
var options = new RetryOptions
{
    ExceptionFilter = ex => ex is HttpRequestException
};

Retry.Execute(() => SendRequest(), options);
```

### 6. Jitter and Observability
Add jitter to prevent "thundering herd" and hook into retry events.

```csharp
var options = new RetryOptions
{
    DelayStrategy = RetryDelays.WithJitter(RetryDelays.Fixed(TimeSpan.FromSeconds(1))),
    OnRetry = ctx => Console.WriteLine($"Retry {ctx.Attempt} due to {ctx.Exception.Message}")
};

Retry.Execute(() => CriticalTask(), options);
```

## Performance Benchmarks

`RetrySharp` is significantly faster than general-purpose libraries like `Polly`, especially on the critical path.

| Method                                   | Mean         | Allocated |
|----------------------------------------- |-------------:|----------:|
| **RetrySharp_Sync_FastPath (State)**     | **1.00 us**  | **0 B**   |
| **RetrySharp_Async_FastPath (State)**    | **2.09 us**  | **0 B**   |
| Polly_Sync_FastPath                      | 11.43 us     | 64 B      |
| Polly_Async_FastPath                     | 10.95 us     | 64 B      |
| **RetrySharp_Sync_OneRetry**             | **18.28 us** | **320 B** |
| Polly_Sync_OneRetry                      | 31.90 us     | 608 B     |

*Benchmarks run on .NET 10.0.5, Intel Xeon CPU E31225 3.10GHz.*

## Core Principles

1. **Simplicity:** No complex inheritance or policy composition.
2. **Performance:** Zero allocations in the hot path via state-based overloads.
3. **Safety:** Do not swallow `OperationCanceledException` or system-critical errors.

## License

Licensed under the [MIT License](LICENSE).
