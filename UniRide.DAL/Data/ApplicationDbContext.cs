using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using UniRide.DAL.Models;


namespace UniRide.DAL.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<AcademicProfile> AcademicProfiles => Set<AcademicProfile>();
        public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - AcademicProfile (1:1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.AcademicProfile)
                .WithOne(p => p.User)
                .HasForeignKey<AcademicProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - DriverProfile (1:1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.DriverProfile)
                .WithOne(p => p.User)
                .HasForeignKey<DriverProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - SupervisorProfile (1:1)
            

            // Optional: unique email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
             .HasIndex(u => u.Phone)
             .IsUnique()
             .HasFilter("[Phone] IS NOT NULL");
        }


    }
}
