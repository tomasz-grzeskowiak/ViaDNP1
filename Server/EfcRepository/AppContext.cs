using Entities;
using Microsoft.EntityFrameworkCore;

namespace EfcRepository;

public class AppContext : DbContext
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Comment> Comments => Set<Comment>();

    public AppContext(DbContextOptions<AppContext> options) : base(options)
    {
    }
}