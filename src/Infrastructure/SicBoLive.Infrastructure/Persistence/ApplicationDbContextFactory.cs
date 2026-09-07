using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SicBoLive.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations` construct the context at design time without spinning up the WebApi host.</summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite("Data Source=sicbolive.db");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
