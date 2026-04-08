using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ComputerRepairService.Models.Entities;

namespace ComputerRepairService.Data
{
    public class RepairDbContext : IdentityDbContext<ApplicationUser>
    {
        public RepairDbContext(DbContextOptions<RepairDbContext> options) : base(options)
        {
        }

        // DbSet для всех сущностей
        public DbSet<Customer> Customers { get; set; }
        public DbSet<DeviceType> DeviceTypes { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<OrderPart> OrderParts { get; set; }
        public DbSet<OrderService> OrderServices { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }
        public DbSet<OrderTechnician> OrderTechnicians { get; set; }
        public DbSet<PartCategory> PartCategories { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceOrder> ServiceOrders { get; set; }
        public DbSet<Technician> Technicians { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Связь ApplicationUser -> Customer (1:1)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(au => au.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь ApplicationUser -> Technician (1:1)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(au => au.Technician)
                .WithOne(t => t.User)
                .HasForeignKey<Technician>(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь ServiceOrder -> Customer
            modelBuilder.Entity<ServiceOrder>()
                .HasOne(so => so.Customer)
                .WithMany(c => c.ServiceOrders)
                .HasForeignKey(so => so.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь ServiceOrder -> DeviceType
            modelBuilder.Entity<ServiceOrder>()
                .HasOne(so => so.DeviceType)
                .WithMany(dt => dt.ServiceOrders)
                .HasForeignKey(so => so.DeviceTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь ServiceOrder -> OrderStatus
            modelBuilder.Entity<ServiceOrder>()
                .HasOne(so => so.OrderStatus)
                .WithMany(os => os.ServiceOrders)
                .HasForeignKey(so => so.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка для OrderTechnician (связь многие-ко-многим между ServiceOrder и Technician)
            modelBuilder.Entity<OrderTechnician>()
                .HasOne(ot => ot.ServiceOrder)
                .WithMany(so => so.OrderTechnicians)
                .HasForeignKey(ot => ot.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderTechnician>()
                .HasOne(ot => ot.Technician)
                .WithMany(t => t.OrderTechnicians)
                .HasForeignKey(ot => ot.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка для OrderService
            modelBuilder.Entity<OrderService>()
                .HasOne(os => os.ServiceOrder)
                .WithMany(so => so.OrderServices)
                .HasForeignKey(os => os.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderService>()
                .HasOne(os => os.Service)
                .WithMany(s => s.OrderServices)
                .HasForeignKey(os => os.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderService>()
                .HasOne(os => os.Technician)
                .WithMany(t => t.OrderServices)
                .HasForeignKey(os => os.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка для OrderPart
            modelBuilder.Entity<OrderPart>()
                .HasOne(op => op.ServiceOrder)
                .WithMany(so => so.OrderParts)
                .HasForeignKey(op => op.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderPart>()
                .HasOne(op => op.Inventory)
                .WithMany(i => i.OrderParts)
                .HasForeignKey(op => op.PartId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка для Inventory
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.PartCategory)
                .WithMany(pc => pc.Inventories)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка для OrderStatusHistory
            modelBuilder.Entity<OrderStatusHistory>()
                .HasOne(osh => osh.ServiceOrder)
                .WithMany(so => so.OrderStatusHistories)
                .HasForeignKey(osh => osh.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderStatusHistory>()
                .HasOne(osh => osh.OrderStatus)
                .WithMany(os => os.OrderStatusHistories)
                .HasForeignKey(osh => osh.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка для Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ServiceOrder)
                .WithMany(so => so.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Установка значений по умолчанию
            modelBuilder.Entity<ServiceOrder>()
                .Property(so => so.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<ServiceOrder>()
                .Property(so => so.StatusId)
                .HasDefaultValue(1);

            modelBuilder.Entity<ServiceOrder>()
                .Property(so => so.Priority)
                .HasDefaultValue(1);

            modelBuilder.Entity<ServiceOrder>()
                .Property(so => so.TotalCost)
                .HasDefaultValue(0);

            modelBuilder.Entity<Customer>()
                .Property(c => c.RegistrationDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Customer>()
                .Property(c => c.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Technician>()
                .Property(t => t.HireDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Technician>()
                .Property(t => t.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Inventory>()
                .Property(i => i.QuantityInStock)
                .HasDefaultValue(0);

            modelBuilder.Entity<Inventory>()
                .Property(i => i.ReorderLevel)
                .HasDefaultValue(5);

            modelBuilder.Entity<Inventory>()
                .Property(i => i.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Service>()
                .Property(s => s.IsActive)
                .HasDefaultValue(true);

            // Настройка длин строк для Identity (опционально)
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FirstName)
                    .HasMaxLength(100);

                entity.Property(u => u.LastName)
                    .HasMaxLength(100);

                entity.Property(u => u.Address)
                    .HasMaxLength(500);
            });
        }
    }
}