using GreenStock.Logging;
using GreenStock.Models;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace GreenStock.Data;



public class AppDbContext : DbContext
{
    private static readonly ILogger _log = AppLogger.For<AppDbContext>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();

    public DbSet<Contractor> Contractors => Set<Contractor>();

    public DbSet<ContractorCheckHistory> ContractorCheckHistories => Set<ContractorCheckHistory>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public AppDbContext() { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            if (DbConfig.UseInMemory)
            {
                _log.Debug("AppDbContext: InMemory-режим (тесты)");
                optionsBuilder.UseInMemoryDatabase(DbConfig.InMemoryDbName);
            }
            else
            {
                _log.Debug("AppDbContext: конфигурация из DbConfig.ConnectionString");
                optionsBuilder.UseNpgsql(DbConfig.ConnectionString);
            }
        }
    }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users").HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnType("uuid").ValueGeneratedOnAdd();
            e.Property(u => u.Role).HasConversion<string>().HasColumnName("role");
            e.HasMany(u => u.Shipments).WithOne(s => s.CreatedByUser).HasForeignKey(s => s.CreatedBy);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories").HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnType("uuid").ValueGeneratedOnAdd();
            e.HasMany(c => c.Products).WithOne(p => p.Category).HasForeignKey(p => p.CategoryId);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products").HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnType("uuid").ValueGeneratedOnAdd();
            e.Property(p => p.CategoryId).HasColumnType("uuid");
            e.Property(p => p.ExpiryDate).HasColumnName("expiry_date").IsRequired(false);
            e.Property(p => p.SellingPrice).HasColumnName("selling_price").HasDefaultValue(0m);
        });

        modelBuilder.Entity<Shipment>(e =>
        {
            e.ToTable("shipments").HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnType("uuid").ValueGeneratedOnAdd();
            e.Property(s => s.CreatedBy).HasColumnType("uuid");
            e.Property(s => s.Recipient).HasColumnName("recipient").HasMaxLength(200);
            e.HasMany(s => s.Items).WithOne(i => i.Shipment).HasForeignKey(i => i.ShipmentId);
        });

        modelBuilder.Entity<ShipmentItem>(e =>
        {
            e.ToTable("shipment_items").HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnType("uuid").ValueGeneratedOnAdd();
            e.Property(i => i.ShipmentId).HasColumnType("uuid");
            e.Property(i => i.ProductId).HasColumnType("uuid");
            e.Property(i => i.Price).HasColumnName("price").HasDefaultValue(0m);
        });

        modelBuilder.Entity<Contractor>(e =>
        {
            e.ToTable("contractors").HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnType("uuid").ValueGeneratedOnAdd();
            e.Property(c => c.Inn).HasMaxLength(12);
            e.HasIndex(c => c.Inn).IsUnique();
        });

        modelBuilder.Entity<ContractorCheckHistory>(e =>
        {
            e.ToTable("contractor_check_history").HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnType("uuid").ValueGeneratedOnAdd();
            e.HasOne(h => h.Contractor)
                .WithMany()
                .HasForeignKey(h => h.ContractorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}