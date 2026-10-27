using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Library.Infra.EF.Context;

public interface IApplicationDbContext : IDatabaseContext
{ }

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
