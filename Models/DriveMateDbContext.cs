using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using adminPage.Models;
using System.Linq;


public class DriveMateDbContext : DbContext
{
    public DriveMateDbContext(DbContextOptions<DriveMateDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Driver> Drivers { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map User.PasswordHash to existing DB column 'Password' if present
        modelBuilder.Entity<User>(b =>
        {
            b.Property(u => u.PasswordHash).HasColumnName("Password");
        });

        base.OnModelCreating(modelBuilder);
    }
}