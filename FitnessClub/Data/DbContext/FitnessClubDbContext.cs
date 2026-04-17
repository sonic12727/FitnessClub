using FitnessClub.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessClub.Data
{
    public class FitnessClubDbContext : DbContext
    {
        public FitnessClubDbContext(DbContextOptions<FitnessClubDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Callback> Callbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique(); // User имеет уникальный Email
                entity.Property(u => u.Email).HasMaxLength(50);
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(20);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(20);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Membership
            modelBuilder.Entity<Membership>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.StartDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(m => m.IsActive).HasDefaultValue(true);
                // User может иметь один активный Membership
                entity.HasOne(m => m.User).WithOne(u => u.Membership).HasForeignKey<Membership>(m => m.UserId);
            });

            // Attendance
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.CheckInTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // User может иметь много Attedances
                entity.HasOne(a => a.User).WithMany(u => u.Attendances).HasForeignKey(a => a.UserId);
            });

            // Callback
            modelBuilder.Entity<Callback>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
                entity.Property(x => x.Phone).IsRequired();
                entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(x => x.IsProcessed).HasDefaultValue(false);
            });
        }
    }
}
