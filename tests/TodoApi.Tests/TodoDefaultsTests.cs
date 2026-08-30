using Xunit;

namespace TodoApi.Tests;

public sealed class TodoDefaultsTests
{
    [Fact]
    public void Default_page_size_is_twenty()
    {
        Assert.Equal(20, TodoDefaults.DefaultPageSize);
    }

    [Fact]
    public void Maximum_page_size_is_one_hundred()
    {
        Assert.Equal(100, TodoDefaults.MaximumPageSize);
    }
}
