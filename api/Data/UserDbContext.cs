using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Models;

namespace TodoApp.Api.Data;

public sealed class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.Property(entity => entity.Name).HasMaxLength(80).IsRequired();
        user.Property(entity => entity.Email).HasMaxLength(254).IsRequired();
        user.Property(entity => entity.Mobile).HasMaxLength(20).IsRequired();
        user.Property(entity => entity.Gender).HasMaxLength(40).IsRequired();
        user.Property(entity => entity.UserType).HasMaxLength(20).IsRequired();
        user.Property(entity => entity.Range).HasColumnType("nvarchar(max)");
        modelBuilder.Entity<UserCredential>(credential =>
        {
            credential.HasKey(entity => entity.UserId);
            credential.Property(entity => entity.PasswordHash).HasMaxLength(512).IsRequired();
            credential.HasOne<User>()
                .WithOne()
                .HasForeignKey<UserCredential>(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RevokedToken>(token =>
        {
            token.HasKey(entity => entity.TokenId);
            token.Property(entity => entity.TokenId).HasMaxLength(36);
            token.HasIndex(entity => entity.ExpiresAt);
        });
        user.HasData(new User
        {
            Id = 1,
            Name = "Ankit Sahu",
            Email = "ankit@gmail.com",
            Mobile = "8978786933",
            Gender = "Male",
            Dob = new DateTime(1992, 8, 31),
            IsActive = true,
            Range = [0, 10],
            UserType = "Admin",
        });
    }
}
