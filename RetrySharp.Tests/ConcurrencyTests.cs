using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RetrySharp.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task Execute_ParallelCalls_AreIndependent()
    {
        const int ParallelCount = 100;
        var tasks = Enumerable.Range(0, ParallelCount).Select(i => Task.Run(() =>
        {
            int calls = 0;
            Retry.Execute(() =>
            {
                calls++;
                if (calls < 2) throw new Exception();
            }, new RetryOptions { MaxAttempts = 2 });
            return calls;
        }));

        var results = await Task.WhenAll(tasks);
        Assert.All(results, r => Assert.Equal(2, r));
    }

    [Fact]
    public async Task ExecuteAsync_ParallelCalls_AreIndependent()
    {
        const int ParallelCount = 100;
        var tasks = Enumerable.Range(0, ParallelCount).Select(async i =>
        {
            int calls = 0;
            await Retry.ExecuteAsync(async ct =>
            {
                calls++;
                await Task.Yield();
                if (calls < 2) throw new Exception();
            }, new RetryOptions { MaxAttempts = 2 });
            return calls;
        });

        var results = await Task.WhenAll(tasks);
        Assert.All(results, r => Assert.Equal(2, r));
    }
}
