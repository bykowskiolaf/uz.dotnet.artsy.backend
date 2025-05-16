using artsy.backend.Models;
using Microsoft.EntityFrameworkCore;

namespace artsy.backend.Data;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
	}

	public DbSet<User> Users { get; set; }
	public DbSet<RefreshToken> RefreshTokens { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// User configurations
		modelBuilder.Entity<User>()
			.HasIndex(u => u.Email)
			.IsUnique();
		modelBuilder.Entity<User>()
			.HasIndex(u => u.Username)
			.IsUnique();
		
		// RefreshToken configurations
		modelBuilder.Entity<RefreshToken>(entity =>
		{
			entity.HasOne(rt => rt.User)
				.WithMany(u => u.RefreshTokens)
				.HasForeignKey(rt => rt.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasIndex(rt => rt.Token).IsUnique();
		});
	}
}
