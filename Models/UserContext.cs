using Hospital.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.Models
{
    public class UserContext: DbContext
    {
        public UserContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryUser> CategoryUsers { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CategoryUser>()
                .HasKey(uc => new { uc.UserId, uc.CategoryId });

            modelBuilder.Entity<CategoryUser>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.CategoryUsers)
                .HasForeignKey(uc => uc.UserId);

            modelBuilder.Entity<CategoryUser>()
                .HasOne(uc => uc.Category)
                .WithMany(c => c.CategoryUsers)
                .HasForeignKey(uc => uc.CategoryId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
