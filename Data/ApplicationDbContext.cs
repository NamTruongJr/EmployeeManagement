
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Web.Models;

namespace EmployeeManagement.Web.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Phải gọi base để Identity tạo cấu hình cho các bảng AspNet...
        base.OnModelCreating(modelBuilder);

        // Department configuration
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasQueryFilter(d => !d.IsDeleted);

            entity.HasIndex(d => d.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        // Employee configuration
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasIndex(e => e.EmployeeCode)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Position)
                .WithMany()
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LeaveRequest configuration
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(l => l.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // Position configuration
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);

            entity.HasIndex(p => p.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });
    }
}
