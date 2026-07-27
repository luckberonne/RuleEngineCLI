using FluentAssertions;
using RuleEngineCLI.Infrastructure.Performance;
using Xunit;

namespace RuleEngineCLI.Infrastructure.Tests.Performance;

public class ObjectPoolTests
{
    private sealed class PooledItem
    {
        public bool WasReset { get; set; }
    }

    [Fact]
    public void Rent_WhenPoolEmpty_CreatesNewInstance()
    {
        var pool = new ObjectPool<PooledItem>(() => new PooledItem());

        var item = pool.Rent();

        item.Should().NotBeNull();
        pool.Count.Should().Be(0);
    }

    [Fact]
    public void Return_ThenRent_ReusesSameInstance()
    {
        var pool = new ObjectPool<PooledItem>(() => new PooledItem());
        var item = pool.Rent();

        pool.Return(item);
        var reused = pool.Rent();

        reused.Should().BeSameAs(item);
        pool.Count.Should().Be(0);
    }

    [Fact]
    public void Return_InvokesResetAction()
    {
        var pool = new ObjectPool<PooledItem>(() => new PooledItem(), resetAction: i => i.WasReset = true);
        var item = pool.Rent();

        pool.Return(item);

        item.WasReset.Should().BeTrue();
    }

    [Fact]
    public void Return_BeyondMaxSize_DropsExcessObjects()
    {
        var pool = new ObjectPool<PooledItem>(() => new PooledItem(), maxSize: 1);

        pool.Return(new PooledItem());
        pool.Return(new PooledItem());

        pool.Count.Should().Be(1);
    }

    [Fact]
    public void Clear_RemovesAllPooledObjects()
    {
        var pool = new ObjectPool<PooledItem>(() => new PooledItem());
        pool.Return(new PooledItem());
        pool.Return(new PooledItem());

        pool.Clear();

        pool.Count.Should().Be(0);
    }

    [Fact]
    public void RentScoped_ReturnsObjectToPoolOnDispose()
    {
        var pool = new ObjectPool<PooledItem>(() => new PooledItem());

        using (var scoped = pool.RentScoped())
        {
            scoped.Value.Should().NotBeNull();
        }

        pool.Count.Should().Be(1);
    }
}
