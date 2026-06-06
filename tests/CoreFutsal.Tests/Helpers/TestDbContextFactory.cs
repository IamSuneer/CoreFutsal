using CoreFutsal.DAL;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Tests.Helpers;

public static class TestDbContextFactory
{
    public static FutsalContext Create(string? name = null)
    {
        var options = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;

        return new FutsalContext(options);
    }
}
