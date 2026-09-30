using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.Entities;
using Route = WebApplication1.Models.Entities.Route;

namespace WebApplication1.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Bus> Buses { get; set; }

    public virtual DbSet<BusStop> BusStops { get; set; }

    public virtual DbSet<BusType> BusTypes { get; set; }

    public virtual DbSet<Driver> Drivers { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Route> Routes { get; set; }

    public virtual DbSet<RouteStop> RouteStops { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<Trip> Trips { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VehicleLocation> VehicleLocations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__73951AED344B333B");

            entity.ToTable("Booking");

            entity.HasIndex(e => e.BookingCode, "UQ__Booking__C6E56BD5AA712EE1").IsUnique();

            entity.Property(e => e.BookingCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.BookingTime).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Trip).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.TripId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Trip");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_User");
        });

        modelBuilder.Entity<Bus>(entity =>
        {
            entity.HasKey(e => e.BusId).HasName("PK__Bus__6A0F60B5208354AC");

            entity.ToTable("Bus");

            entity.HasIndex(e => e.LicensePlate, "UQ__Bus__026BC15CA01E33BD").IsUnique();

            entity.Property(e => e.LicensePlate)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.BusType).WithMany(p => p.Buses)
                .HasForeignKey(d => d.BusTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bus_BusType");
        });

        modelBuilder.Entity<BusStop>(entity =>
        {
            entity.HasKey(e => e.StopId).HasName("PK__BusStop__EB6A38F475F4CB16");

            entity.ToTable("BusStop");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.StopName).HasMaxLength(150);
        });

        modelBuilder.Entity<BusType>(entity =>
        {
            entity.HasKey(e => e.BusTypeId).HasName("PK__BusType__84A10CE81EAFE5EA");

            entity.ToTable("BusType");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.DriverId).HasName("PK__Driver__F1B1CD04BC800BDD");

            entity.ToTable("Driver");

            entity.HasIndex(e => e.UserId, "UQ__Driver__1788CC4DB0203D2A").IsUnique();

            entity.Property(e => e.LicenseNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithOne(p => p.Driver)
                .HasForeignKey<Driver>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Driver_User");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E1265721E85");

            entity.ToTable("Notification");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_User");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A3815265BE5");

            entity.ToTable("Payment");

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PaymentTime).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TransactionCode)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Booking");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1ABEB337AD");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B61609C6ED659").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(e => e.RouteId).HasName("PK__Route__80979B4D3B1AB5F3");

            entity.ToTable("Route");

            entity.HasIndex(e => e.RouteCode, "UQ__Route__FDC3458509A2F8D5").IsUnique();

            entity.Property(e => e.Distance).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.EndPoint).HasMaxLength(150);
            entity.Property(e => e.RouteCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RouteName).HasMaxLength(100);
            entity.Property(e => e.StartPoint).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RouteStop>(entity =>
        {
            entity.HasKey(e => e.RouteStopId).HasName("PK__RouteSto__37A9A5FAF58DFED5");

            entity.ToTable("RouteStop");

            entity.HasIndex(e => new { e.RouteId, e.StopOrder }, "UQ_RouteStop_Route_Order").IsUnique();

            entity.HasIndex(e => new { e.RouteId, e.StopId }, "UQ_RouteStop_Route_Stop").IsUnique();

            entity.HasOne(d => d.Route).WithMany(p => p.RouteStops)
                .HasForeignKey(d => d.RouteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RouteStop_Route");

            entity.HasOne(d => d.Stop).WithMany(p => p.RouteStops)
                .HasForeignKey(d => d.StopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RouteStop_BusStop");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B4917336653");

            entity.ToTable("Schedule");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Route).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RouteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Schedule_Route");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__Ticket__712CC607CAAAC3DE");

            entity.ToTable("Ticket");

            entity.HasIndex(e => e.TicketCode, "UQ__Ticket__598CF7A322DD0DD1").IsUnique();

            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TicketCode)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.BoardingStop).WithMany(p => p.TicketBoardingStops)
                .HasForeignKey(d => d.BoardingStopId)
                .HasConstraintName("FK_Ticket_BoardingStop");

            entity.HasOne(d => d.Booking).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ticket_Booking");

            entity.HasOne(d => d.DropOffStop).WithMany(p => p.TicketDropOffStops)
                .HasForeignKey(d => d.DropOffStopId)
                .HasConstraintName("FK_Ticket_DropOffStop");
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(e => e.TripId).HasName("PK__Trip__51DC713E99BD592E");

            entity.ToTable("Trip");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Bus).WithMany(p => p.Trips)
                .HasForeignKey(d => d.BusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trip_Bus");

            entity.HasOne(d => d.Driver).WithMany(p => p.Trips)
                .HasForeignKey(d => d.DriverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trip_Driver");

            entity.HasOne(d => d.Route).WithMany(p => p.Trips)
                .HasForeignKey(d => d.RouteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trip_Route");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Trips)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("FK_Trip_Schedule");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4CE5B44E4A");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__A9D105344D844B95").IsUnique();

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserRole_Role"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserRole_User"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF2760AD61B02829");
                        j.ToTable("UserRole");
                    });
        });

        modelBuilder.Entity<VehicleLocation>(entity =>
        {
            entity.HasKey(e => e.LocationId).HasName("PK__VehicleL__E7FEA497F9BC7EBC");

            entity.ToTable("VehicleLocation");

            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.RecordedAt).HasColumnType("datetime");
            entity.Property(e => e.Speed).HasColumnType("decimal(6, 2)");

            entity.HasOne(d => d.Trip).WithMany(p => p.VehicleLocations)
                .HasForeignKey(d => d.TripId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VehicleLocation_Trip");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
