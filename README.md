# RetrySharp

**RetrySharp** is a lightweight, high-performance retry utility for C# with zero external dependencies. Designed for systems where every microsecond and allocation matters.

## Key Features

*   **Near-Zero Overhead:** Optimized fast-path for non-retry scenarios.
*   **Zero Allocations:** Target 0 allocations in synchronous hot paths.
*   **Sync & Async:** Native support for both execution paths.
*   **Predictable:** No hidden behavior or complex policy chaining.
*   **Minimalist:** Tiny API surface (learnable in minutes).

## Installation

```bash
dotnet add package RetrySharp
```

## Usage

### Basic Retry

Retries up to 3 times (default) if any exception occurs.

```csharp
Retry.Execute(() => DoWork());
```

### Async Retry with Cancellation

Supports `CancellationToken` and `ConfigureAwait(false)` internally.

```csharp
await Retry.ExecuteAsync(async ct => 
{
    await DoDownloadAsync(ct);
}, cancellationToken: cts.Token);
```

### Exponential Backoff

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

### Exception Filtering

Only retry for specific exception types.

```csharp
var options = new RetryOptions
{
    ExceptionFilter = ex => ex is HttpRequestException
};

Retry.Execute(() => SendRequest(), options);
```

### Jitter and Observability

Add jitter to prevent "thundering herd" and hook into retry events.

```csharp
var options = new RetryOptions
{
    DelayStrategy = RetryDelays.WithJitter(RetryDelays.Fixed(TimeSpan.FromSeconds(1))),
    OnRetry = ctx => Console.WriteLine($"Retry {ctx.Attempt} due to {ctx.Exception.Message}")
};

Retry.Execute(() => CriticalTask(), options);
```

## Performance

RetrySharp is built for performance-critical applications:
*   **Fast-path optimization:** When `MaxAttempts` is 1 and no extra options are set, the execution is a direct call with zero library overhead.
*   **Struct-based context:** `RetryContext` is a `readonly struct` to avoid heap allocations during callbacks.
*   **Thread-safe:** All delay strategies and jitter mechanisms are thread-safe.

## License

Licensed under the [MIT License](LICENSE).
