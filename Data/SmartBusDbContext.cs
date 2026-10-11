using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.Entities;
using Route = WebApplication1.Models.Entities.Route;

namespace WebApplication1.Data
{
    public class SmartBusDbContext : DbContext
    {
        public SmartBusDbContext(DbContextOptions<SmartBusDbContext> options)
            : base(options)
        {
        }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<BusStop> BusStops { get; set; }
        public DbSet<BusType> BusTypes { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<VehicleLocation> VehicleLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Ánh xạ chính xác tên bảng trong SQL Server (dạng số ít)
            modelBuilder.Entity<Booking>().ToTable("Booking");
            modelBuilder.Entity<Bus>().ToTable("Bus");
            modelBuilder.Entity<BusStop>().ToTable("BusStop").HasKey(s => s.StopId);
            modelBuilder.Entity<BusType>().ToTable("BusType");
            modelBuilder.Entity<Driver>().ToTable("Driver");
            modelBuilder.Entity<Notification>().ToTable("Notification");
            modelBuilder.Entity<Payment>().ToTable("Payment");
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<Route>().ToTable("Route");
            modelBuilder.Entity<RouteStop>().ToTable("RouteStop");
            modelBuilder.Entity<Schedule>().ToTable("Schedule");
            modelBuilder.Entity<Ticket>().ToTable("Ticket");
            modelBuilder.Entity<Trip>().ToTable("Trip");
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<UserRole>().ToTable("UserRole");
            modelBuilder.Entity<VehicleLocation>().ToTable("VehicleLocation").HasKey(vl => vl.LocationId);

            // 2. Khóa chính phức hợp (Composite Key) cho UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Quan hệ Driver -> User
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.User)
                .WithOne(u => u.Driver)
                .HasForeignKey<Driver>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Quan hệ Bus -> BusType
            modelBuilder.Entity<Bus>()
                .HasOne(b => b.BusType)
                .WithMany(bt => bt.Buses)
                .HasForeignKey(b => b.BusTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Quan hệ RouteStop
            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.Route)
                .WithMany(r => r.RouteStops)
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.BusStop)
                .WithMany(s => s.RouteStops)
                .HasForeignKey(rs => rs.StopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RouteStop>()
                .HasIndex(rs => new { rs.RouteId, rs.StopId })
                .IsUnique();

            modelBuilder.Entity<RouteStop>()
                .HasIndex(rs => new { rs.RouteId, rs.StopOrder })
                .IsUnique();

            // 6. Quan hệ Schedule -> Route
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Route)
                .WithMany(r => r.Schedules)
                .HasForeignKey(s => s.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // 7. Quan hệ Trip
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Route)
                .WithMany(r => r.Trips)
                .HasForeignKey(t => t.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Bus)
                .WithMany(b => b.Trips)
                .HasForeignKey(t => t.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Driver)
                .WithMany(d => d.Trips)
                .HasForeignKey(t => t.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Schedule)
                .WithMany(s => s.Trips)
                .HasForeignKey(t => t.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            // 8. Quan hệ Booking
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Trip)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            // 9. Quan hệ Ticket
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Booking)
                .WithMany(b => b.Tickets)
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.BoardingStop)
                .WithMany()
                .HasForeignKey(t => t.BoardingStopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.DropOffStop)
                .WithMany()
                .HasForeignKey(t => t.DropOffStopId)
                .OnDelete(DeleteBehavior.Restrict);

            // 10. Quan hệ Payment -> Booking
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // 11. Quan hệ Notification -> User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 12. Quan hệ VehicleLocation -> Trip
            modelBuilder.Entity<VehicleLocation>()
                .HasOne(vl => vl.Trip)
                .WithMany(t => t.VehicleLocations)
                .HasForeignKey(vl => vl.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            // 13. Cấu hình độ chính xác Decimal & Kiểu dữ liệu đặc thù SQL Server
            modelBuilder.Entity<Route>()
                .Property(r => r.Distance)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalAmount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Price)
                .HasPrecision(12, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<BusStop>()
                .Property(bs => bs.Latitude)
                .HasPrecision(10, 6);

            modelBuilder.Entity<BusStop>()
                .Property(bs => bs.Longitude)
                .HasPrecision(10, 6);

            modelBuilder.Entity<VehicleLocation>()
                .Property(vl => vl.Latitude)
                .HasPrecision(10, 6);

            modelBuilder.Entity<VehicleLocation>()
                .Property(vl => vl.Longitude)
                .HasPrecision(10, 6);

            modelBuilder.Entity<VehicleLocation>()
                .Property(vl => vl.Speed)
                .HasPrecision(6, 2);

            modelBuilder.Entity<Trip>()
                .Property(t => t.TripDate)
                .HasColumnType("date");

            modelBuilder.Entity<Trip>()
                .Property(t => t.DepartureTime)
                .HasColumnType("time");

            modelBuilder.Entity<Trip>()
                .Property(t => t.ArrivalTime)
                .HasColumnType("time");

            modelBuilder.Entity<RouteStop>()
                .Property(rs => rs.ArrivalTime)
                .HasColumnType("time");

            modelBuilder.Entity<RouteStop>()
                .Property(rs => rs.DepartureTime)
                .HasColumnType("time");

            modelBuilder.Entity<Schedule>()
                .Property(s => s.DepartureTime)
                .HasColumnType("time");

            modelBuilder.Entity<Driver>()
                .Property(d => d.LicenseExpiryDate)
                .HasColumnType("date");
        }
    }
}
