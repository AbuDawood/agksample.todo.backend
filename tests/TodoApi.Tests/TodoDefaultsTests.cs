using Xunit;

namespace TodoApi.Tests;

public sealed class TodoDefaultsTests
{
    [Fact]
    public void Default_page_size_is_twenty()
    {
        Assert.Equal(20, TodoDefaults.DefaultPageSize);
    }
}
