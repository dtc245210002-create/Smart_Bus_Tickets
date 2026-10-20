using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Services;
using BusRoute = WebApplication1.Models.Entities.Route;

namespace SmartBus.G0.TestSupport;

public sealed record FixtureIds(int CustomerA, int CustomerB, int DriverUser, int AdminUser,
    int Trip, int ControlTrip, int PendingBooking, int PaidBooking,
    string PendingTicketCode, string PaidTicketCode, DateOnly ServiceDate);

public sealed class TestDatabase
{
    public string Name { get; }
    public string ConnectionString { get; }
    public string OwnerToken { get; }
    public bool IsEphemeral { get; }
    public FixtureIds Ids { get; private set; } = null!;
    public static DateOnly BangkokToday => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
    public static string DefaultFixtureName => $"{TestDbGuard.Prefix}{BangkokToday:yyyyMMdd}_FixtureV1";

    private TestDatabase(string name, string token, bool ephemeral)
    {
        Name = name; OwnerToken = token; IsEphemeral = ephemeral;
        ConnectionString = TestDbGuard.ConnectionString(name);
    }

    public ApplicationDbContext OpenContext()
    {
        TestDbGuard.Validate(ConnectionString);
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString).Options);
    }

    public static async Task<TestDatabase> CreateAsync(string? name = null, bool ephemeral = false)
    {
        var db = new TestDatabase(name ?? $"{TestDbGuard.Prefix}R_{Guid.NewGuid():N}", Guid.NewGuid().ToString("N"), ephemeral);
        await using var master = new SqlConnection(TestDbGuard.MasterConnection(db.ConnectionString));
        await master.OpenAsync();
        await using var exists = new SqlCommand("SELECT DB_ID(@name)", master);
        exists.Parameters.AddWithValue("@name", db.Name);
        var existing = await exists.ExecuteScalarAsync() is not DBNull and not null;
        await using var context = db.OpenContext();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(context.Database.GenerateCreateScript())));
        if (existing)
        {
            if (ephemeral) throw new InvalidOperationException("G0_EPHEMERAL_NAME_COLLISION");
            // No EF/schema/seed writes until the existing database has passed the marker and schema checks.
            await using var sql = new SqlConnection(db.ConnectionString); await sql.OpenAsync();
            await using var command = new SqlCommand("SELECT Purpose,OwnerToken,IsEphemeral,SeedVersion,SchemaFingerprint,DatabaseName FROM g0.EnvironmentMarker WHERE Id=1", sql);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync() || reader.GetString(0) != TestDbGuard.Purpose || reader.GetBoolean(2)
                || reader.GetString(3) != TestDbGuard.SeedVersion || reader.GetString(4) != fingerprint
                || reader.GetString(5) != db.Name)
                throw new InvalidOperationException("G0_EXISTING_DATABASE_NOT_OWNED_OR_SCHEMA_CHANGED");
            db = new TestDatabase(name!, reader.GetString(1), false);
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
            await using var sql = new SqlConnection(db.ConnectionString); await sql.OpenAsync();
            await using var command = new SqlCommand("""
                CREATE SCHEMA g0;
                """, sql);
            await command.ExecuteNonQueryAsync();
            command.CommandText = """
                CREATE TABLE g0.EnvironmentMarker (
                    Id int NOT NULL PRIMARY KEY CHECK(Id=1),
                    Purpose nvarchar(80) NOT NULL, DatabaseName nvarchar(128) NOT NULL,
                    OwnerToken varchar(32) NOT NULL, IsEphemeral bit NOT NULL,
                    SeedVersion varchar(40) NOT NULL, ContractVersion varchar(20) NOT NULL,
                    SchemaFingerprint varchar(64) NOT NULL, CreatedAtUtc datetime2 NOT NULL
                );
                CREATE TABLE g0.RaceGate (RunId varchar(32) NOT NULL,WorkerId varchar(16) NOT NULL,
                    ReachedAtUtc datetime2 NOT NULL,PRIMARY KEY(RunId,WorkerId));
                INSERT g0.EnvironmentMarker VALUES(1,@purpose,@name,@token,@ephemeral,@seed,@contract,@fingerprint,SYSUTCDATETIME());
                """;
            command.Parameters.AddWithValue("@purpose", TestDbGuard.Purpose);
            command.Parameters.AddWithValue("@name", db.Name);
            command.Parameters.AddWithValue("@token", db.OwnerToken);
            command.Parameters.AddWithValue("@ephemeral", db.IsEphemeral);
            command.Parameters.AddWithValue("@seed", TestDbGuard.SeedVersion);
            command.Parameters.AddWithValue("@contract", TestDbGuard.ContractVersion);
            command.Parameters.AddWithValue("@fingerprint", fingerprint);
            await command.ExecuteNonQueryAsync();
        }
        await db.SeedAsync();
        return db;
    }

    public static async Task<TestDatabase> AttachWorkerAsync(string name, string token)
    {
        var db = new TestDatabase(name, token, true);
        await db.AssertOwnedAsync(requireEphemeral: true);
        await using var context = db.OpenContext();
        db.Ids = await LoadIdsAsync(context);
        return db;
    }

    public async Task AssertOwnedAsync(bool requireEphemeral = false)
    {
        TestDbGuard.Validate(ConnectionString);
        await using var sql = new SqlConnection(ConnectionString); await sql.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM g0.EnvironmentMarker WHERE Id=1 AND Purpose=@purpose AND DatabaseName=@name AND OwnerToken=@token AND (@ephemeral=0 OR IsEphemeral=1)", sql);
        command.Parameters.AddWithValue("@purpose", TestDbGuard.Purpose);
        command.Parameters.AddWithValue("@name", Name);
        command.Parameters.AddWithValue("@token", OwnerToken);
        command.Parameters.AddWithValue("@ephemeral", requireEphemeral);
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("G0_MARKER_REJECTED");
    }

    public async Task SeedAsync()
    {
        await AssertOwnedAsync();
        await using var context = OpenContext();
        // Re-opening a canonical fixture does not reset data or wipe experiments.
        if (await context.Routes.AnyAsync()) { Ids = await LoadIdsAsync(context); return; }
        await using var tx = await context.Database.BeginTransactionAsync();
        var customerRole = new Role { RoleName = "Khách hàng" };
        var driverRole = new Role { RoleName = "Tài xế" };
        var adminRole = new Role { RoleName = "Admin" };
        User User(string key, string name, Role role) => new()
        {
            Email = $"g0.{key}@example.invalid", FullName = name,
            PasswordHash = PasswordHasher.HashPassword("G0FixtureOnly!"),
            Status = "Active", CreatedAt = DateTime.UtcNow, Roles = new List<Role> { role }
        };
        var a = User("customer.a", "G0 Khách A", customerRole);
        var b = User("customer.b", "G0 Khách B", customerRole);
        var driverUser = User("driver", "G0 Tài xế", driverRole);
        var adminUser = User("admin", "G0 Điều hành", adminRole);
        var driver = new Driver { User = driverUser, LicenseNo = "G0-NOT-A-REAL-LICENSE", Status = "Active" };
        var type = new BusType { TypeName = "Xe ghế ngồi 29 chỗ", Description = "G0 fixture only" };
        var bus = new Bus { BusType = type, Capacity = 29, LicensePlate = "G0-TEST-001", Status = "Active" };
        var origin = new BusStop { StopName = "G0 Hà Nội", Address = "Địa chỉ giả để kiểm thử" };
        var destination = new BusStop { StopName = "G0 Hải Phòng", Address = "Địa chỉ giả để kiểm thử" };
        var route = new BusRoute { RouteCode = "G0-HN-HP", RouteName = "G0 Hà Nội - Hải Phòng", StartPoint = "Hà Nội", EndPoint = "Hải Phòng", Distance = 120, EstimatedDuration = 120, Status = "Active" };
        route.RouteStops.Add(new RouteStop { Stop = origin, StopOrder = 1, DepartureTime = new TimeOnly(8, 0) });
        route.RouteStops.Add(new RouteStop { Stop = destination, StopOrder = 2, ArrivalTime = new TimeOnly(10, 0) });
        var serviceDate = BangkokToday.AddDays(2);
        Trip Trip(DateOnly date) => new() { Route = route, Bus = bus, Driver = driver, TripDate = date, DepartureTime = new TimeOnly(8, 0), ArrivalTime = new TimeOnly(10, 0), Status = "Scheduled" };
        var trip = Trip(serviceDate); var control = Trip(serviceDate.AddDays(1));
        var pending = new Booking { User = a, Trip = trip, BookingCode = "G0-PENDING", BookingTime = DateTime.Now, TotalAmount = 200000, Status = "Pending" };
        pending.Tickets.Add(new Ticket { TicketCode = "G0-PENDING-A01", SeatNumber = "A01", Price = 200000, Status = "HELD", BoardingStop = origin, DropOffStop = destination });
        var paid = new Booking { User = b, Trip = control, BookingCode = "G0-PAID", BookingTime = DateTime.Now, TotalAmount = 600000, Status = "Confirmed" };
        foreach (var seat in new[] { "A01", "A02", "A03" })
            paid.Tickets.Add(new Ticket { TicketCode = "G0-PAID-" + seat, SeatNumber = seat, Price = 200000, Status = "ISSUED", BoardingStop = origin, DropOffStop = destination });
        paid.Payments.Add(new Payment { Amount = 600000, PaymentMethod = "G0-SANDBOX", TransactionCode = "G0-SEED-NOT-A-REAL-PAYMENT", PaymentTime = DateTime.UtcNow, Status = "Paid" });
        context.AddRange(pending, paid, adminUser);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        Ids = await LoadIdsAsync(context);
    }

    private static async Task<FixtureIds> LoadIdsAsync(ApplicationDbContext context)
    {
        var pending = await context.Bookings.AsNoTracking().SingleAsync(b => b.BookingCode == "G0-PENDING");
        var paid = await context.Bookings.AsNoTracking().SingleAsync(b => b.BookingCode == "G0-PAID");
        var users = await context.Users.AsNoTracking().Where(u => u.Email.StartsWith("g0.")).ToDictionaryAsync(u => u.Email, u => u.UserId);
        var trip = await context.Trips.AsNoTracking().SingleAsync(t => t.TripId == pending.TripId);
        return new(users["g0.customer.a@example.invalid"], users["g0.customer.b@example.invalid"], users["g0.driver@example.invalid"], users["g0.admin@example.invalid"], pending.TripId, paid.TripId, pending.BookingId, paid.BookingId, "G0-PENDING-A01", "G0-PAID-A01", trip.TripDate);
    }

    public async Task<object> DescribeAsync()
    {
        await AssertOwnedAsync(); await using var db = OpenContext();
        return new { database = Name, server = TestDbGuard.Instance, purpose = TestDbGuard.Purpose,
            contractVersion = TestDbGuard.ContractVersion, seedVersion = TestDbGuard.SeedVersion,
            ephemeral = IsEphemeral, timezone = "Asia/Bangkok", ids = Ids,
            counts = new { users = await db.Users.CountAsync(), routes = await db.Routes.CountAsync(),
                stops = await db.BusStops.CountAsync(), buses = await db.Buses.CountAsync(), trips = await db.Trips.CountAsync(),
                bookings = await db.Bookings.CountAsync(), tickets = await db.Tickets.CountAsync(), payments = await db.Payments.CountAsync() } };
    }

    public async Task DropOwnedEphemeralAsync()
    {
        if (!IsEphemeral || !Name.StartsWith(TestDbGuard.Prefix + "R_", StringComparison.Ordinal))
            throw new InvalidOperationException("G0_REFUSE_TO_DROP_PERSISTENT_FIXTURE");
        await AssertOwnedAsync(requireEphemeral: true);
        // An independently generated per-test catalog, exact name and owner token only. Never enumerate/drop arbitrary DBs.
        SqlConnection.ClearAllPools();
        await using var master = new SqlConnection(TestDbGuard.MasterConnection(ConnectionString)); await master.OpenAsync();
        await using var command = new SqlCommand($"ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Name}];", master);
        await command.ExecuteNonQueryAsync();
    }
}
