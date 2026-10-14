using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LicenseMaster> Licenses => Set<LicenseMaster>();
    public DbSet<LicenseAssignment> LicenseAssignments => Set<LicenseAssignment>();
    public DbSet<Renewal> Renewals => Set<Renewal>();
    public DbSet<AuditHistory> AuditHistories => Set<AuditHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User indexes & relationships
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<User>(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Employee indexes & relationships
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Manager)
                .WithMany(m => m.Subordinates)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LicenseMaster indexes
        modelBuilder.Entity<LicenseMaster>(entity =>
        {
            entity.HasIndex(l => l.LicenseCode).IsUnique();
            entity.HasIndex(l => l.SoftwareName);
            entity.HasIndex(l => l.Status);
            entity.HasIndex(l => l.ExpiryDate);
        });

        // LicenseAssignment relationships & indexes
        modelBuilder.Entity<LicenseAssignment>(entity =>
        {
            entity.HasIndex(a => new { a.EmployeeId, a.LicenseId, a.Status });

            entity.HasOne(a => a.Employee)
                .WithMany(e => e.LicenseAssignments)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.License)
                .WithMany(l => l.LicenseAssignments)
                .HasForeignKey(a => a.LicenseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AssignedByUser)
                .WithMany()
                .HasForeignKey(a => a.AssignedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.RevokedByUser)
                .WithMany()
                .HasForeignKey(a => a.RevokedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Renewal relationships
        modelBuilder.Entity<Renewal>(entity =>
        {
            entity.HasOne(r => r.License)
                .WithMany(l => l.Renewals)
                .HasForeignKey(r => r.LicenseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.RenewedByUser)
                .WithMany()
                .HasForeignKey(r => r.RenewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AuditHistory relationships
        modelBuilder.Entity<AuditHistory>(entity =>
        {
            entity.HasIndex(a => a.DateTime);
            entity.HasIndex(a => a.Module);
            entity.HasIndex(a => a.Action);

            entity.HasOne(a => a.User)
                .WithMany(u => u.AuditHistories)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Notification relationships
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => n.CreatedAt);
            entity.HasIndex(n => new { n.UserId, n.IsRead });

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
