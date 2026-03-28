using System;
using System.Threading;
using System.Threading.Tasks;
using RetrySharp;

namespace RetryConsole;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== RetrySharp Examples ===");

        // Example 1: Basic Synchronous Retry
        Example1_BasicSync();

        // Example 2: Async Retry with Exponential Backoff and Jitter
        await Example2_AsyncExponentialJitter();

        // Example 3: Filtering Exceptions
        Example3_FilteredRetry();

        // Example 4: Using the Fast Path (Zero overhead)
        Example4_FastPath();

        Console.WriteLine("\n=== Examples Completed ===");
    }

    private static void Example1_BasicSync()
    {
        Console.WriteLine("\n--- Example 1: Basic Sync ---");
        int calls = 0;
        Retry.Execute(() =>
        {
            calls++;
            Console.WriteLine($"  Attempt {calls}...");
            if (calls < 3) throw new Exception("Transient failure");
            Console.WriteLine("  Success!");
        });
    }

    private static async Task Example2_AsyncExponentialJitter()
    {
        Console.WriteLine("\n--- Example 2: Async Exponential + Jitter ---");
        
        var options = new RetryOptions
        {
            MaxAttempts = 4,
            DelayStrategy = RetryDelays.WithJitter(
                RetryDelays.Exponential(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(2))),
            OnRetry = ctx => Console.WriteLine($"  Retrying... Attempt {ctx.Attempt} failed. Next delay: {ctx.Delay?.TotalMilliseconds:F0}ms. Error: {ctx.Exception.Message}")
        };

        int calls = 0;
        await Retry.ExecuteAsync(async ct =>
        {
            calls++;
            Console.WriteLine($"  Async call {calls}...");
            await Task.Delay(50, ct); // Simulate work
            if (calls < 3) throw new InvalidOperationException("Network timeout");
            Console.WriteLine("  Async success!");
        }, options);
    }

    private static void Example3_FilteredRetry()
    {
        Console.WriteLine("\n--- Example 3: Filtered Retry (Only ArgumentException) ---");
        
        var options = new RetryOptions
        {
            MaxAttempts = 3,
            ExceptionFilter = ex => ex is ArgumentException
        };

        try
        {
            Retry.Execute(() =>
            {
                Console.WriteLine("  Executing action that throws InvalidOperationException...");
                throw new InvalidOperationException("I should not be retried");
            }, options);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  Caught expected immediate failure: {ex.Message}");
        }
    }

    private static void Example4_FastPath()
    {
        Console.WriteLine("\n--- Example 4: Fast Path (Zero Overhead) ---");
        
        // When MaxAttempts is 1 and no extra options are set, RetrySharp invokes the delegate directly.
        Retry.Execute(() => 
        {
            Console.WriteLine("  Direct execution with zero library overhead.");
        }, new RetryOptions { MaxAttempts = 1 });
    }
}
