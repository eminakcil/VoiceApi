using Microsoft.EntityFrameworkCore;
using VoiceApi.Domain;

namespace VoiceApi.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>().Property(u => u.Id).ValueGeneratedNever();

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // RefreshToken Configuration
        modelBuilder.Entity<RefreshToken>().Property(rt => rt.Id).ValueGeneratedNever();

        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        // RefreshToken Configuration - Relationship
        modelBuilder
            .Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global Query Filter for Soft Delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(
                        nameof(SetSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Static
                    )!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder)
        where T : class, ISoftDelete
    {
        modelBuilder.Entity<T>().HasQueryFilter(x => !x.IsDeleted);
    }
}
