using System;
using System.Threading;
using System.Threading.Tasks;

namespace RetrySharp;

public static class Retry
{
    public static void Execute(Action action, RetryOptions? options = null)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        options ??= RetryOptions.Default;

        // Fast path
        if (options.MaxAttempts <= 1 && options.DelayStrategy == null && options.ExceptionFilter == null && options.OnRetry == null)
        {
            action();
            return;
        }

        int attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                action();
                return;
            }
            catch (Exception ex) when (attempt < options.MaxAttempts && RetryOptions.IsRetriable(ex) && (options.ExceptionFilter == null || options.ExceptionFilter(ex)))
            {
                HandleRetry(attempt, ex, options);
            }
        }
    }

    public static T Execute<T>(Func<T> func, RetryOptions? options = null)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        options ??= RetryOptions.Default;

        // Fast path
        if (options.MaxAttempts <= 1 && options.DelayStrategy == null && options.ExceptionFilter == null && options.OnRetry == null)
        {
            return func();
        }

        int attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                return func();
            }
            catch (Exception ex) when (attempt < options.MaxAttempts && RetryOptions.IsRetriable(ex) && (options.ExceptionFilter == null || options.ExceptionFilter(ex)))
            {
                HandleRetry(attempt, ex, options);
            }
        }
    }

    private static void HandleRetry(int attempt, Exception ex, RetryOptions options)
    {
        TimeSpan? delay = options.DelayStrategy?.Invoke(attempt, ex);

        if (options.OnRetry != null)
        {
            try
            {
                options.OnRetry(new RetryContext(attempt, ex, delay));
            }
            catch
            {
                // OnRetry must not interrupt retry flow
            }
        }

        if (delay.HasValue && delay.Value > TimeSpan.Zero)
        {
            Thread.Sleep(delay.Value);
        }
    }

    public static async Task ExecuteAsync(Func<CancellationToken, Task> func, RetryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        options ??= RetryOptions.Default;

        int attempt = 0;
        while (true)
        {
            attempt++;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await func(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attempt < options.MaxAttempts && RetryOptions.IsRetriable(ex) && (options.ExceptionFilter == null || options.ExceptionFilter(ex)))
            {
                await HandleRetryAsync(attempt, ex, options, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> func, RetryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        options ??= RetryOptions.Default;

        int attempt = 0;
        while (true)
        {
            attempt++;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await func(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < options.MaxAttempts && RetryOptions.IsRetriable(ex) && (options.ExceptionFilter == null || options.ExceptionFilter(ex)))
            {
                await HandleRetryAsync(attempt, ex, options, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task HandleRetryAsync(int attempt, Exception ex, RetryOptions options, CancellationToken cancellationToken)
    {
        TimeSpan? delay = options.DelayStrategy?.Invoke(attempt, ex);

        if (options.OnRetry != null)
        {
            try
            {
                options.OnRetry(new RetryContext(attempt, ex, delay));
            }
            catch
            {
                // OnRetry must not interrupt retry flow
            }
        }

        if (delay.HasValue && delay.Value > TimeSpan.Zero)
        {
            await Task.Delay(delay.Value, cancellationToken).ConfigureAwait(false);
        }
    }
}
