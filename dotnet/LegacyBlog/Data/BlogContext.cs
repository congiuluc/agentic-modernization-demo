using LegacyBlog.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyBlog.Data;

public class BlogContext(DbContextOptions<BlogContext> options) : DbContext(options)
{
    public DbSet<BlogUser> Users => Set<BlogUser>();
    public DbSet<BlogRole> Roles => Set<BlogRole>();
    public DbSet<BlogUserRole> UserRoles => Set<BlogUserRole>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<SeedState> SeedStates => Set<SeedState>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<BlogUser>(e =>
        {
            e.ToTable("AspNetUsers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(128);
            e.Property(x => x.UserName).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.UserName).IsUnique().HasDatabaseName("UserNameIndex");
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.LockoutEndDateUtc).HasColumnType("datetime");
        });
        model.Entity<BlogRole>(e =>
        {
            e.ToTable("AspNetRoles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(128);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("RoleNameIndex");
        });
        model.Entity<BlogUserRole>(e =>
        {
            e.ToTable("AspNetUserRoles");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.Property(x => x.UserId).HasMaxLength(128);
            e.Property(x => x.RoleId).HasMaxLength(128);
            e.HasOne(x => x.User).WithMany(x => x.Roles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId);
        });
        model.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasMany(x => x.Posts).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Post>(e =>
        {
            e.ToTable("Posts");
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.Published, x.PublishedAt }).HasDatabaseName("IX_Publication");
            e.Property(x => x.CreatedAt).HasColumnType("datetime");
            e.Property(x => x.UpdatedAt).HasColumnType("datetime");
            e.Property(x => x.PublishedAt).HasColumnType("datetime");
            e.HasMany(x => x.Comments).WithOne(x => x.Post).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<Comment>(e => { e.ToTable("Comments"); e.Property(x => x.CreatedAt).HasColumnType("datetime"); });
        model.Entity<SeedState>().ToTable("SeedStates");
        // EF6 Identity 2 used these tables even when they contain no records.
        model.Entity<BlogUserClaim>(e => { e.ToTable("AspNetUserClaims"); e.HasKey(x => x.Id); e.Property(x => x.UserId).HasMaxLength(128); });
        model.Entity<BlogUserLogin>(e => { e.ToTable("AspNetUserLogins"); e.HasKey(x => new { x.LoginProvider, x.ProviderKey, x.UserId }); e.Property(x => x.LoginProvider).HasMaxLength(128); e.Property(x => x.ProviderKey).HasMaxLength(128); e.Property(x => x.UserId).HasMaxLength(128); });
    }
}

public class BlogUserClaim
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string? ClaimType { get; set; }
    public string? ClaimValue { get; set; }
}
public class BlogUserLogin
{
    public string LoginProvider { get; set; } = "";
    public string ProviderKey { get; set; } = "";
    public string UserId { get; set; } = "";
}
