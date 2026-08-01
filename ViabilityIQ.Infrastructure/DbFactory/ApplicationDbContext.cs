using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser table
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("tblApplicationUsers");

                entity.Property(u => u.Id).HasColumnName("UserId");
                entity.Property(u => u.FirstName).HasMaxLength(255);
                entity.Property(u => u.LastName).HasMaxLength(255);
                entity.Property(u => u.Department).HasMaxLength(255);
                entity.Property(u => u.JobTitle).HasMaxLength(255);
                entity.Property(u => u.Address).HasMaxLength(500);
                entity.Property(u => u.City).HasMaxLength(255);
                entity.Property(u => u.Country).HasMaxLength(255);
            });

            // Configure ApplicationRole table
            builder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("tblApplicationRoles");

                entity.Property(r => r.Id).HasColumnName("RoleId");
                entity.Property(r => r.Description).HasMaxLength(500);
            });

            // Configure UserRole junction table (Many-to-Many relationship)
            builder.Entity<IdentityUserRole<long>>(entity =>
            {
                entity.ToTable("tblApplicationUserRoles");

                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<ApplicationRole>()
                    .WithMany()
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure UserClaim table
            builder.Entity<IdentityUserClaim<long>>(entity =>
            {
                entity.ToTable("tblApplicationUserClaims");
                entity.Property(uc => uc.Id).HasColumnName("UserClaimId");
            });

            // Configure UserLogin table
            builder.Entity<IdentityUserLogin<long>>(entity =>
            {
                entity.ToTable("tblApplicationUserLogins");
            });

            // Configure RoleClaim table
            builder.Entity<IdentityRoleClaim<long>>(entity =>
            {
                entity.ToTable("tblApplicationRoleClaims");
                entity.Property(rc => rc.Id).HasColumnName("RoleClaimId");
            });

            // Configure UserToken table
            builder.Entity<IdentityUserToken<long>>(entity =>
            {
                entity.ToTable("tblApplicationUserTokens");
            });

            // Seed default roles
            SeedRoles(builder);
        }

        private static void SeedRoles(ModelBuilder builder)
        {
            var adminRoleId = 1L;
            var userRoleId = 2L;
            var moderatorRoleId = 3L;

            builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "Administrator role with full access",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new ApplicationRole
                {
                    Id = userRoleId,
                    Name = "User",
                    NormalizedName = "USER",
                    Description = "Standard user role",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new ApplicationRole
                {
                    Id = moderatorRoleId,
                    Name = "Moderator",
                    NormalizedName = "MODERATOR",
                    Description = "Moderator role with limited admin access",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );
        }
    }
}