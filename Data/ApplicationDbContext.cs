using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EmployeeDirectory.Models;
using Microsoft.AspNetCore.Identity;

namespace EmployeeDirectory.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
	{
	}

	public DbSet<Employee> Employees { get; set; }
	public DbSet<Department> Departments { get; set; }
	public DbSet<AuditLog> AuditLogs { get; set; }
	public DbSet<OtpVerification> OtpVerifications { get; set; }

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.Entity<Employee>(entity =>
		{
			entity.HasKey(e => e.EmployeeId);
			entity.HasIndex(e => e.Email).IsUnique();
			entity.HasOne(e => e.Department)
				.WithMany(d => d.Employees)
				.HasForeignKey(e => e.DepartmentId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(e => e.Manager)
				.WithMany()
				.HasForeignKey(e => e.ManagerId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<Department>(entity =>
		{
			entity.HasKey(d => d.DepartmentId);
			entity.HasOne(d => d.Head)
				.WithMany()
				.HasForeignKey(d => d.HeadEmployeeId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<AuditLog>(entity =>
		{
			entity.HasKey(a => a.AuditId);
			entity.Property(a => a.Timestamp).HasDefaultValueSql("GETUTCDATE()");
			entity.HasIndex(a => a.Timestamp);
			entity.HasOne(a => a.ActingUser)
				.WithMany()
				.HasForeignKey(a => a.ActingUserId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<OtpVerification>(entity =>
		{
			entity.HasKey(o => o.OtpVerificationId);
			entity.HasIndex(o => new { o.UserId, o.GeneratedAt });
			entity.HasIndex(o => o.ExpiresAt);
			entity.HasOne(o => o.User)
				.WithMany()
				.HasForeignKey(o => o.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<ApplicationUser>(entity =>
		{
			entity.Property(u => u.IsActive).HasDefaultValue(true);
		});
	}
}
