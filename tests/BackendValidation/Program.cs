using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartBus.G0.TestSupport;
using WebApplication1.Controllers.Api;
using WebApplication1.Models.Api;
using WebApplication1.Models.Entities;
using WebApplication1.Services;

if (args.FirstOrDefault() == "worker")
{
    var workerDb = await TestDatabase.AttachWorkerAsync(args[1], args[2]);
    var uid = int.Parse(args[3]); var trip = int.Parse(args[4]); var run = args[5];
    await using (var sql = new SqlConnection(workerDb.ConnectionString))
    {
        await sql.OpenAsync();
        await using var command = new SqlCommand("INSERT g0.RaceGate VALUES(@run,@worker,SYSUTCDATETIME())", sql);
        command.Parameters.AddWithValue("@run", run); command.Parameters.AddWithValue("@worker", uid.ToString());
        await command.ExecuteNonQueryAsync();
        command.CommandText = "SELECT COUNT(*) FROM g0.RaceGate WHERE RunId=@run";
        var timer = Stopwatch.StartNew();
        while (Convert.ToInt32(await command.ExecuteScalarAsync()) < 2)
        { if (timer.Elapsed > TimeSpan.FromSeconds(15)) throw new Exception("Worker start barrier timed out"); await Task.Delay(25); }
    }
    await using var ctx = workerDb.OpenContext();
    var controller = new BookingsApiController(ctx, new SeatHoldService())
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, uid.ToString()) }, "test")) } }
    };
    var result = await controller.HoldSeats(new HoldSeatsRequest { TripId = trip, SeatNumbers = new() { "A06" }, UserId = uid });
    Console.WriteLine(JsonSerializer.Serialize(new { worker = uid, status = (result.Result as ObjectResult)?.StatusCode ?? 200 }));
    return;
}

var results = new List<object>(); var failures = 0;
void Check(string name, bool passed, object? detail = null)
{ results.Add(new { name, passed, detail }); Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {name}"); if (!passed) failures++; }
var root = Environment.GetEnvironmentVariable("SMARTBUS_VALIDATION_ROOT")
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var resultsDir = Environment.GetEnvironmentVariable("SMARTBUS_VALIDATION_RESULTS")
    ?? Path.Combine(root, "TestResults/backend-validation");
var product = Path.Combine(root, "bin/Debug/net9.0/WebApplication1.dll");
var database = await TestDatabase.CreateAsync(ephemeral: true);
var processes = new List<Process>(); var logs = new List<string>();
int FreePort()
{ var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
Process Launch(string file, IEnumerable<string> arguments, string? connection = null, string environment = "Testing")
{
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false,
        RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
    start.ArgumentList.Add(file); foreach (var arg in arguments) start.ArgumentList.Add(arg);
    start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
    if (connection != null) start.Environment["ConnectionStrings__DefaultConnection"] = connection;
    var proc = Process.Start(start)!; processes.Add(proc);
    proc.OutputDataReceived += (_, e) => { if (e.Data != null) lock (logs) logs.Add(e.Data); };
    proc.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (logs) logs.Add(e.Data); };
    proc.BeginOutputReadLine(); proc.BeginErrorReadLine(); return proc;
}
HttpClient Client(int port) => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
{ BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(25) };
async Task WaitForServer(HttpClient client)
{
    for (var attempt = 0; attempt < 100; attempt++)
    { try { using var r = await client.GetAsync("/Account/Login"); if ((int)r.StatusCode == 200) return; } catch (HttpRequestException) { } await Task.Delay(200); }
    throw new Exception("Test server did not start");
}
async Task<HttpResponseMessage> Login(HttpClient client, string email, string password, bool driver = false)
{
    var path = driver ? "/Driver/Login" : "/Account/Login";
    var html = await client.GetStringAsync(path);
    var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    if (token.Length == 0) throw new Exception("Antiforgery token missing");
    return await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
    { [driver ? "DriverIdentifier" : "Username"] = email, ["Password"] = password, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token) }));
}
async Task<object> Snapshot()
{
    await using var c = database.OpenContext();
    return new { users = await c.Users.CountAsync(), trips = await c.Trips.CountAsync(), bookings = await c.Bookings.CountAsync(),
        tickets = await c.Tickets.CountAsync(), payments = await c.Payments.CountAsync(),
        states = await c.Tickets.OrderBy(t => t.TicketId).Select(t => new { t.TicketId, t.Status, t.BookingId, t.SeatNumber, t.Price }).ToListAsync(),
        bookingStates = await c.Bookings.OrderBy(b => b.BookingId).Select(b => new { b.BookingId, b.UserId, b.BookingCode, b.BookingTime, b.TotalAmount, b.Status }).ToListAsync() };
}
async Task<JsonElement> HoldSnapshot(HttpClient client, string extra = "") =>
    await client.GetFromJsonAsync<JsonElement>("/api/bookings/trip-seat-status?tripId=" + database.Ids.Trip + extra);

// Browser QA uses the same guarded disposable fixture as HTTP regressions.
// No connection string, ownership token or real credentials are written to metadata.
if (args.FirstOrDefault() == "browser-fixture")
{
    var minutes = args.Length > 1 && int.TryParse(args[1], out var requested) ? Math.Clamp(requested, 5, 90) : 60;
    var browserDir = Path.Combine(root, "TestResults/browser-fixture");
    Directory.CreateDirectory(browserDir);
    var runId = Guid.NewGuid().ToString("N");
    var stopPath = Path.Combine(browserDir, runId + ".stop");
    var metadataPath = Path.Combine(browserDir, "context.json");
    var expires = DateTimeOffset.UtcNow.AddMinutes(minutes);
    var browserEnvironment = args.ElementAtOrDefault(2) == "Production" ? "Production" : "Development";
    var browserProduct = browserEnvironment == "Production"
        ? Path.GetFullPath(Path.Combine(root, "../publish-release-check/WebApplication1.dll")) : product;
    var browserPort = FreePort();
    var baseUrl = $"http://127.0.0.1:{browserPort}";
    var status = "STARTING";
    var dropped = false;
    async Task Metadata() => await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(new
    {
        runId, status, baseUrl, expiresAtUtc = expires, runnerPid = Environment.ProcessId, environment = browserEnvironment,
        stopPath, databaseDropped = dropped, fixture = database.Ids,
        searchUrl = baseUrl + "/Trip/Search?from=H%C3%A0%20N%E1%BB%99i&to=H%E1%BA%A3i%20Ph%C3%B2ng&date=" + database.Ids.ServiceDate.ToString("yyyy-MM-dd"),
        accounts = new[] { "g0.customer.a@example.invalid", "g0.customer.b@example.invalid", "g0.driver@example.invalid", "g0.admin@example.invalid" },
        credentialsSource = "tests/BackendValidation/TestSupport/TestDatabase.cs (synthetic fixture only)",
        warning = "Local disposable browser test; never use real customer data or real bank transfers."
    }, new JsonSerializerOptions { WriteIndented = true }));
    try
    {
        await Metadata();
        if (!File.Exists(browserProduct)) throw new FileNotFoundException("Build/publish the browser test target first", browserProduct);
        Launch(browserProduct, new[] { "--urls", baseUrl, "--contentRoot", browserEnvironment == "Production" ? Path.GetDirectoryName(browserProduct)! : root }, database.ConnectionString, browserEnvironment);
        using var browserClient = Client(browserPort);
        await WaitForServer(browserClient);
        status = "READY"; await Metadata();
        Console.WriteLine($"BROWSER_FIXTURE_READY {baseUrl} metadata={metadataPath}");
        while (DateTimeOffset.UtcNow < expires && !File.Exists(stopPath) && processes.All(p => !p.HasExited))
            await Task.Delay(1000);
    }
    finally
    {
        foreach (var process in processes)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(); process.Dispose();
        }
        await database.DropOwnedEphemeralAsync(); dropped = true;
        status = "STOPPED"; await Metadata();
        await File.WriteAllLinesAsync(Path.Combine(browserDir, runId + ".log"), logs);
        Console.WriteLine("BROWSER_FIXTURE_STOPPED owned temporary database dropped");
    }
    return;
}
try
{
    var port = FreePort(); Launch(product, new[] { "--urls", $"http://127.0.0.1:{port}" }, database.ConnectionString);
    using var anon = Client(port); await WaitForServer(anon);
    using var a = Client(port); using var b = Client(port); using var driver = Client(port); using var admin = Client(port);
    Check("Password: real legacy hash accepted", PasswordHasher.VerifyPassword("G0FixtureOnly!", PasswordHasher.HashPassword("G0FixtureOnly!")));
    Check("Password: wrong/demo/plain/hash-as-password rejected", !PasswordHasher.VerifyPassword("wrong", PasswordHasher.HashPassword("right"))
        && !PasswordHasher.VerifyPassword("123456", "demo_hash") && !PasswordHasher.VerifyPassword("123456", "PASS_HASH")
        && !PasswordHasher.VerifyPassword("123456", "hashed_pass_1") && !PasswordHasher.VerifyPassword("plain", "plain")
        && !PasswordHasher.VerifyPassword(PasswordHasher.HashPassword("right"), PasswordHasher.HashPassword("right")));
    Check("Anonymous admin denied", (int)(await anon.GetAsync("/Admin/Dashboard")).StatusCode == 302);
    Check("Anonymous hold API 401", (int)(await anon.PostAsJsonAsync("/api/bookings/hold-seats", new { tripId = database.Ids.Trip, seatNumbers = new[] { "A02" } })).StatusCode == 401);
    Check("Customer A login", (int)(await Login(a, "g0.customer.a@example.invalid", "G0FixtureOnly!")).StatusCode == 302);
    Check("Customer B login", (int)(await Login(b, "g0.customer.b@example.invalid", "G0FixtureOnly!")).StatusCode == 302);
    Check("Driver real login", (int)(await Login(driver, "g0.driver@example.invalid", "G0FixtureOnly!", true)).StatusCode == 302);
    Check("Admin real login", (int)(await Login(admin, "g0.admin@example.invalid", "G0FixtureOnly!")).StatusCode == 302);
    Check("Customer cannot access admin", (int)(await a.GetAsync("/Admin/Dashboard")).StatusCode == 403);
    Check("Driver not elevated to admin", (int)(await driver.GetAsync("/Admin/Dashboard")).StatusCode == 403);
    Check("Admin can access admin", (int)(await admin.GetAsync("/Admin/Dashboard")).StatusCode == 200);
    Check("Customer cannot access driver/dashboard/demo", (int)(await a.GetAsync("/Driver/Dashboard")).StatusCode == 403
        && (int)(await a.GetAsync("/DriverDemo/Dashboard")).StatusCode == 403);
    foreach (var path in new[] { "/Ticket/Detail?id=" + database.Ids.PaidTicketCode,
        "/Ticket/Cancel?id=" + database.Ids.PaidTicketCode, "/Ticket/Reschedule?ticketCode=" + database.Ids.PaidTicketCode,
        "/api/v1/tickets/" + database.Ids.PaidTicketCode + "/change-eligibility",
        "/api/payment/check-status?ticketCode=" + database.Ids.PaidTicketCode })
        Check("Cross-owner denied: " + path.Split('?')[0], (int)(await a.GetAsync(path)).StatusCode == 404);
    Check("Own ticket readable", (int)(await b.GetAsync("/Ticket/Detail?id=" + database.Ids.PaidTicketCode)).StatusCode == 200);
    var inspectBefore = JsonSerializer.Serialize(await Snapshot());
    var inspected = await admin.PostAsJsonAsync("/api/v1/tickets/inspect-qr", new { qrPayload = database.Ids.PaidTicketCode, currentTripId = database.Ids.ControlTrip });
    var inspectedBody = await inspected.Content.ReadFromJsonAsync<JsonElement>();
    Check("Authorized inspect succeeds without check-in", (int)inspected.StatusCode == 200 && inspectedBody.GetProperty("success").GetBoolean()
        && inspectedBody.GetProperty("data").GetProperty("status").GetString() == "ISSUED");
    Check("Inspect does not invent check-in timestamp", inspectedBody.GetProperty("data").GetProperty("checkInTime").GetDateTime() == default);
    await admin.PostAsJsonAsync("/api/v1/tickets/inspect-qr", new { qrPayload = database.Ids.PaidTicketCode, currentTripId = database.Ids.ControlTrip });
    Check("Inspect repeated leaves all SQL rows/statuses unchanged", inspectBefore == JsonSerializer.Serialize(await Snapshot()));
    Check("Inspect rejects fuzzy booking-prefix code", (int)(await admin.PostAsJsonAsync("/api/v1/tickets/inspect-qr",
        new { qrPayload = "G0-PAID", currentTripId = database.Ids.ControlTrip })).StatusCode == 404);
    var scanCode = "G0-PAID-A03";
    var checkIn = await admin.PostAsJsonAsync("/api/v1/tickets/validate-qr", new { qrPayload = scanCode, currentTripId = database.Ids.ControlTrip });
    Check("Explicit authorized check-in still works", (int)checkIn.StatusCode == 200);
    Check("Duplicate explicit check-in remains blocked", (int)(await admin.PostAsJsonAsync("/api/v1/tickets/validate-qr",
        new { qrPayload = scanCode, currentTripId = database.Ids.ControlTrip })).StatusCode == 409);
    var usedInspectBefore = JsonSerializer.Serialize(await Snapshot());
    Check("Inspect USED returns already-used without another check-in", (int)(await admin.PostAsJsonAsync("/api/v1/tickets/inspect-qr",
        new { qrPayload = scanCode, currentTripId = database.Ids.ControlTrip })).StatusCode == 409 && usedInspectBefore == JsonSerializer.Serialize(await Snapshot()));
    Check("Missing booking is rejected", (int)(await a.GetAsync("/Ticket/Payment")).StatusCode == 400);
    var before = JsonSerializer.Serialize(await Snapshot());
    foreach (var path in new[] { "/Ticket/Detail?id=" + database.Ids.PendingTicketCode,
        "/Ticket/PaymentResult?ticketCode=" + database.Ids.PendingTicketCode + "&status=success&amount=999999",
        "/Driver/Dashboard", "/Trip/Search?from=H%C3%A0%20N%E1%BB%99i&to=H%E1%BA%A3i%20Ph%C3%B2ng&date=" + database.Ids.ServiceDate.ToString("yyyy-MM-dd") })
    { using var r = await (path.StartsWith("/Driver") ? driver : a).GetAsync(path); Check("GET expected response: " + path.Split('?')[0], (int)r.StatusCode == (path.StartsWith("/Ticket/Detail") ? 409 : 200), (int)r.StatusCode); }
    Check("GET refresh does not mint trips/bookings/tickets/payments/statuses", before == JsonSerializer.Serialize(await Snapshot()));
    var pendingHtml = await (await a.GetAsync("/Ticket/Detail?id=" + database.Ids.PendingTicketCode)).Content.ReadAsStringAsync();
    Check("Pending detail has no boarding QR payload", !pendingHtml.Contains("SMARTBUS|TICKET:"));
    Check("Payment GET cannot write", (int)(await a.GetAsync("/Ticket/ConfirmPayment?ticketCode=" + database.Ids.PendingTicketCode + "&amount=1")).StatusCode == 405);
    for (var i = 0; i < 2; i++) Check("Unverified/insufficient/replayed payment rejected " + i,
        (int)(await a.PostAsJsonAsync("/api/bookings/verify-payment", new { ticketCode = database.Ids.PendingTicketCode, amount = 1, transactionCode = "REPLAY" })).StatusCode == 503);
    Check("Unverified payment produces no writes", before == JsonSerializer.Serialize(await Snapshot()));
    Check("Google sample login disabled", (int)(await anon.GetAsync("/Account/ExternalLogin")).StatusCode == 503
        && (int)(await anon.GetAsync("/GoogleAuth/SelectAccount")).StatusCode == 503);
    using var badDriver = Client(port);
    Check("Driver sample password rejected", (int)(await Login(badDriver, "g0.driver@example.invalid", "123456", true)).StatusCode == 200
        && (int)(await badDriver.GetAsync("/Driver/Dashboard")).StatusCode == 302);
    await using (var db = database.OpenContext())
    {
        var u = new User { FullName = "Driver2 fixture", Email = "g0.driver2@example.invalid", Status = "Active", PasswordHash = PasswordHasher.HashPassword("G0FixtureOnly!"), CreatedAt = DateTime.UtcNow };
        var other = new Driver { User = u, LicenseNo = "G0-DRIVER-2", Status = "Active" }; db.Drivers.Add(other); await db.SaveChangesAsync();
        var trip = await db.Trips.SingleAsync(t => t.TripId == database.Ids.ControlTrip); trip.DriverId = other.DriverId; await db.SaveChangesAsync();
    }
    Check("Driver cannot scan another driver's trip", (int)(await driver.PostAsJsonAsync("/api/v1/tickets/validate-qr",
        new { qrPayload = database.Ids.PaidTicketCode, currentTripId = database.Ids.ControlTrip })).StatusCode == 403);
    Check("Driver cannot inspect another driver's trip", (int)(await driver.PostAsJsonAsync("/api/v1/tickets/inspect-qr",
        new { qrPayload = database.Ids.PaidTicketCode, currentTripId = database.Ids.ControlTrip })).StatusCode == 403);
    Check("Customer cannot scan", (int)(await a.PostAsJsonAsync("/api/v1/tickets/validate-qr",
        new { qrPayload = database.Ids.PaidTicketCode, currentTripId = database.Ids.ControlTrip })).StatusCode == 403);
    Check("Missing trip rejected before scan", (int)(await driver.PostAsJsonAsync("/api/v1/tickets/validate-qr", new { qrPayload = database.Ids.PendingTicketCode })).StatusCode == 400);
    Check("Nonexistent seat rejected", (int)(await a.PostAsJsonAsync("/api/bookings/hold-seats", new { tripId = database.Ids.Trip, seatNumbers = new[] { "NOT-A-SEAT" } })).StatusCode == 400);
    Check("ISSUED seat rejected", (int)(await a.PostAsJsonAsync("/api/bookings/hold-seats", new { tripId = database.Ids.ControlTrip, seatNumbers = new[] { "A01" } })).StatusCode == 409);
    Check("Hold uses authenticated actor", (int)(await a.PostAsJsonAsync("/api/bookings/hold-seats", new { tripId = database.Ids.Trip, seatNumbers = new[] { "A02", "A03" }, userId = database.Ids.CustomerB, sessionId = "spoofed" })).StatusCode == 200);
    await using (var db = database.OpenContext())
    {
        var rows = await db.Tickets.Include(t => t.Booking).Where(t => t.Booking.TripId == database.Ids.Trip && (t.SeatNumber == "A02" || t.SeatNumber == "A03")).ToListAsync();
        Check("Spoofed userId not stored", rows.Count == 2 && rows.All(t => t.Booking.UserId == database.Ids.CustomerA));
    }
    var quoteBeforeRead = JsonSerializer.Serialize(await Snapshot());
    var heldQuote = await HoldSnapshot(a);
    var originalQuoteTotal = heldQuote.GetProperty("heldTotalPrice").GetDecimal();
    Check("Read-only quote recovers actual owned booking", heldQuote.GetProperty("heldBookingCode").GetString() == "G0-PENDING"
        && heldQuote.GetProperty("myHeldSeats").GetArrayLength() == 3
        && heldQuote.GetProperty("remainingSeconds").GetInt32() is > 0 and <= 600);
    var spoofedQuote = await HoldSnapshot(b, "&userId=" + database.Ids.CustomerA + "&sessionId=actor-" + database.Ids.CustomerA);
    Check("Quote ignores spoofed actor and exposes no other booking", spoofedQuote.GetProperty("heldBookingCode").ValueKind == JsonValueKind.Null
        && spoofedQuote.GetProperty("heldTotalPrice").ValueKind == JsonValueKind.Null
        && spoofedQuote.GetProperty("remainingSeconds").GetInt32() == 0);
    await HoldSnapshot(a);
    Check("Repeated quote GET leaves rows, totals and hold timestamps unchanged", quoteBeforeRead == JsonSerializer.Serialize(await Snapshot()));
    Check("Quote rejects invalid and missing trips", (int)(await a.GetAsync("/api/bookings/trip-seat-status?tripId=0")).StatusCode == 400
        && (int)(await a.GetAsync("/api/bookings/trip-seat-status?tripId=999999")).StatusCode == 404);
    await b.PostAsJsonAsync("/api/bookings/release-seats", new { tripId = database.Ids.Trip, seatNumbers = new[] { "A02" }, userId = database.Ids.CustomerA, sessionId = "actor-" + database.Ids.CustomerA });
    await using (var db = database.OpenContext()) Check("Spoofed release cannot cancel other's hold", await db.Tickets.AnyAsync(t => t.Booking.TripId == database.Ids.Trip && t.SeatNumber == "A02" && t.Status == "HELD"));
    await a.PostAsJsonAsync("/api/bookings/release-seats", new { tripId = database.Ids.Trip, seatNumbers = new[] { "A02" } });
    await a.PostAsJsonAsync("/api/bookings/hold-seats", new { tripId = database.Ids.Trip, seatNumbers = new[] { "A03" } });
    var afterReleaseQuote = await HoldSnapshot(a);
    await using (var db = database.OpenContext())
    {
        var booking = await db.Bookings.Include(bk => bk.Tickets).SingleAsync(bk => bk.BookingId == database.Ids.PendingBooking);
        Check("Release/retry keeps total equal to active tickets", booking.TotalAmount == booking.Tickets.Where(t => t.Status == "HELD").Sum(t => t.Price)
            && booking.Tickets.Count(t => t.SeatNumber == "A03" && t.Status == "HELD") == 1);
        Check("Quote after release recovers reduced server total and same booking", afterReleaseQuote.GetProperty("heldBookingCode").GetString() == booking.BookingCode
            && afterReleaseQuote.GetProperty("heldTotalPrice").GetDecimal() == booking.TotalAmount
            && booking.TotalAmount < originalQuoteTotal && afterReleaseQuote.GetProperty("myHeldSeats").GetArrayLength() == 2);
    }
    await using (var db = database.OpenContext())
    {
        var booking = await db.Bookings.FindAsync(database.Ids.PendingBooking);
        booking!.TotalAmount += 1; await db.SaveChangesAsync();
    }
    var mismatchBefore = JsonSerializer.Serialize(await Snapshot());
    var mismatch = await HoldSnapshot(a);
    Check("Mismatched stored total returns no quote and is not repaired by GET", mismatch.GetProperty("heldBookingCode").ValueKind == JsonValueKind.Null
        && mismatch.GetProperty("heldTotalPrice").ValueKind == JsonValueKind.Null && mismatch.GetProperty("remainingSeconds").GetInt32() == 0
        && mismatchBefore == JsonSerializer.Serialize(await Snapshot()));
    await using (var db = database.OpenContext())
    { var booking = await db.Bookings.FindAsync(database.Ids.PendingBooking); booking!.TotalAmount -= 1; await db.SaveChangesAsync(); }
    await using (var db = database.OpenContext())
    {
        var extra = new Booking { TripId = database.Ids.Trip, UserId = database.Ids.CustomerA, BookingCode = "G0-AMBIGUOUS-QUOTE", BookingTime = DateTime.Now, Status = "Pending", TotalAmount = 100 };
        extra.Tickets.Add(new Ticket { TicketCode = "G0-AMBIGUOUS-QUOTE-A07", SeatNumber = "A07", Price = 100, Status = "HELD" });
        db.Bookings.Add(extra); await db.SaveChangesAsync();
        var ambiguousBefore = JsonSerializer.Serialize(await Snapshot());
        var ambiguous = await HoldSnapshot(a);
        Check("Multiple owned active bookings cannot yield a guessed quote", ambiguous.GetProperty("heldBookingCode").ValueKind == JsonValueKind.Null
            && ambiguous.GetProperty("heldTotalPrice").ValueKind == JsonValueKind.Null
            && ambiguousBefore == JsonSerializer.Serialize(await Snapshot()));
        db.Tickets.RemoveRange(extra.Tickets); db.Bookings.Remove(extra); await db.SaveChangesAsync();
    }
    Check("Payment view reads server booking", (int)(await a.GetAsync("/Ticket/Payment?bookingCode=G0-PENDING&total=1&from=FAKE")).StatusCode == 200);
    var formHtml = await a.GetStringAsync("/Ticket/Reschedule?ticketCode=" + database.Ids.PendingTicketCode);
    var formToken = Regex.Match(formHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    Check("Exchange cannot bypass payment/seat gate", formToken.Length > 0 && (int)(await a.PostAsync("/Ticket/ConfirmReschedule",
        new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = WebUtility.HtmlDecode(formToken),
            ["OldTicket.TicketCode"] = database.Ids.PendingTicketCode, ["SelectedTripId"] = database.Ids.ControlTrip.ToString(),
            ["SelectedNewSeat"] = "A01", ["NewSeatPrice"] = "1" }))).StatusCode == 409);
    Check("Pending ticket cannot check in without verified payment", (int)(await driver.PostAsJsonAsync("/api/v1/tickets/validate-qr",
        new { qrPayload = database.Ids.PendingTicketCode, currentTripId = database.Ids.Trip })).StatusCode == 400);
    var nodePort = FreePort(); Launch(product, new[] { "--urls", $"http://127.0.0.1:{nodePort}" }, database.ConnectionString);
    using var freshNode = Client(nodePort); await WaitForServer(freshNode);
    using var freshActor = Client(nodePort);
    await Login(freshActor, "g0.customer.a@example.invalid", "G0FixtureOnly!");
    var recoveredQuote = await HoldSnapshot(freshActor);
    Check("Fresh process and new session recover same code, total and original TTL", recoveredQuote.GetProperty("heldBookingCode").GetString() == "G0-PENDING"
        && recoveredQuote.GetProperty("heldTotalPrice").GetDecimal() == afterReleaseQuote.GetProperty("heldTotalPrice").GetDecimal()
        && recoveredQuote.GetProperty("remainingSeconds").GetInt32() <= heldQuote.GetProperty("remainingSeconds").GetInt32());
    var freshMap = await (await freshNode.GetAsync("/api/trips/" + database.Ids.Trip + "/seats?userId=" + database.Ids.CustomerA)).Content.ReadFromJsonAsync<JsonElement>();
    Check("Fresh process reads SQL holds; spoofed anonymous mine=false", freshMap.GetProperty("data").GetProperty("seats").EnumerateArray()
        .Any(seat => seat.GetProperty("seatNumber").GetString() == "A03" && seat.GetProperty("status").GetString() == "HOLD" && !seat.GetProperty("isMine").GetBoolean()));
    await using (var db = database.OpenContext())
    { var t = await db.Tickets.SingleAsync(t => t.TicketCode == database.Ids.PaidTicketCode); t.Status = "CANCELLED"; await db.SaveChangesAsync(); }
    var cancelBefore = JsonSerializer.Serialize(await Snapshot());
    Check("Cancelled PaymentResult is readable", (int)(await b.GetAsync("/Ticket/PaymentResult?ticketCode=" + database.Ids.PaidTicketCode + "&status=success")).StatusCode == 200);
    Check("Cancelled ticket not revived by result GET", cancelBefore == JsonSerializer.Serialize(await Snapshot()));
    await using (var db = database.OpenContext())
    {
        db.Tickets.Add(new Ticket { BookingId = database.Ids.PaidBooking, TicketCode = "X-" + database.Ids.PendingTicketCode + "-X", SeatNumber = "A04", Price = 0, Status = "ISSUED" });
        await db.SaveChangesAsync();
    }
    Check("Ambiguous fuzzy match cannot scan across trips", (int)(await driver.PostAsJsonAsync("/api/v1/tickets/validate-qr",
        new { qrPayload = database.Ids.PendingTicketCode, currentTripId = database.Ids.Trip })).StatusCode == 409);
    var runId = Guid.NewGuid().ToString("N"); var workerDll = typeof(Program).Assembly.Location;
    var w1 = Launch(workerDll, new[] { "worker", database.Name, database.OwnerToken, database.Ids.CustomerA.ToString(), database.Ids.Trip.ToString(), runId });
    var w2 = Launch(workerDll, new[] { "worker", database.Name, database.OwnerToken, database.Ids.CustomerB.ToString(), database.Ids.Trip.ToString(), runId });
    await Task.WhenAll(w1.WaitForExitAsync(), w2.WaitForExitAsync());
    await using (var db = database.OpenContext()) Check("Two OS processes: one SQL allocation for A06", w1.ExitCode == 0 && w2.ExitCode == 0
        && await db.Tickets.CountAsync(t => t.Booking.TripId == database.Ids.Trip && t.SeatNumber == "A06" && t.Status == "HELD") == 1,
        new { first = w1.ExitCode, second = w2.ExitCode });
    w1.WaitForExit(); w2.WaitForExit();
    string[] workerLogSnapshot; lock (logs) workerLogSnapshot = logs.ToArray();
    var workerStatuses = workerLogSnapshot.Where(l => l.StartsWith("{\"worker\":"))
        .Select(l => JsonDocument.Parse(l).RootElement.GetProperty("status").GetInt32()).Order().ToArray();
    Check("Two OS workers return one 200 and one 409", workerStatuses.SequenceEqual(new[] { 200, 409 }), workerStatuses);
    Check("Unknown trip does not become mock map", (int)(await anon.GetAsync("/api/trips/999999/seats")).StatusCode == 404);
    Check("Unknown hold trip not replaced with fallback", (int)(await a.PostAsJsonAsync("/api/bookings/hold-seats",
        new { tripId = 999999, seatNumbers = new[] { "A01" } })).StatusCode == 404);
    Check("Seven seats in one request rejected", (int)(await a.PostAsJsonAsync("/api/bookings/hold-seats",
        new { tripId = database.Ids.Trip, seatNumbers = new[] { "A01", "A02", "A03", "A04", "A05", "A06", "A07" } })).StatusCode == 400);
    await using (var db = database.OpenContext()) { var route = await db.Routes.SingleAsync(); route.Distance = null; await db.SaveChangesAsync(); }
    var fareMap = await (await freshNode.GetAsync("/api/trips/" + database.Ids.Trip + "/seats")).Content.ReadFromJsonAsync<JsonElement>();
    await using (var db = database.OpenContext())
    {
        var trip = await db.Trips.Include(t => t.Route).Include(t => t.Bus).ThenInclude(bus => bus.BusType).SingleAsync(t => t.TripId == database.Ids.Trip);
        var expected = RoutePricingService.CalculateTripPrice(RoutePricingService.GetRouteMetrics(trip.Route.StartPoint, trip.Route.EndPoint).Distance, trip.Bus.BusType.TypeName, trip.Bus.Capacity);
        Check("Missing route distance uses same canonical fare as Search", fareMap.GetProperty("data").GetProperty("basePrice").GetDecimal() == expected);
    }
    var noScheduleBefore = JsonSerializer.Serialize(await Snapshot());
    Check("Search unscheduled day returns without seeding", (int)(await anon.GetAsync("/Trip/Search?from=H%C3%A0%20N%E1%BB%99i&to=H%E1%BA%A3i%20Ph%C3%B2ng&date="
        + database.Ids.ServiceDate.AddDays(30).ToString("yyyy-MM-dd"))).StatusCode == 200
        && noScheduleBefore == JsonSerializer.Serialize(await Snapshot()));
    await using (var db = database.OpenContext())
    { var booking = await db.Bookings.FindAsync(database.Ids.PendingBooking); booking!.BookingTime = DateTime.Now.AddMinutes(-11); await db.SaveChangesAsync(); }
    var expiredQuoteBefore = JsonSerializer.Serialize(await Snapshot());
    var expiredQuote = await HoldSnapshot(a);
    Check("Expired booking yields no checkout quote or TTL renewal", expiredQuote.GetProperty("heldBookingCode").ValueKind == JsonValueKind.Null
        && expiredQuote.GetProperty("heldTotalPrice").ValueKind == JsonValueKind.Null && expiredQuote.GetProperty("remainingSeconds").GetInt32() == 0
        && expiredQuoteBefore == JsonSerializer.Serialize(await Snapshot()));
    var expiredMap = await (await freshNode.GetAsync("/api/trips/" + database.Ids.Trip + "/seats")).Content.ReadFromJsonAsync<JsonElement>();
    Check("Expired hold is available after restart", expiredMap.GetProperty("data").GetProperty("seats").EnumerateArray()
        .Any(seat => seat.GetProperty("seatNumber").GetString() == "A03" && seat.GetProperty("status").GetString() == "AVAILABLE"));
    var reclaim = await b.PostAsJsonAsync("/api/bookings/hold-seats", new { tripId = database.Ids.Trip, seatNumbers = new[] { "A03" }, holdDurationMinutes = 30 });
    var reclaimBody = await reclaim.Content.ReadFromJsonAsync<JsonElement>();
    Check("Other actor can reclaim expired hold", (int)reclaim.StatusCode == 200);
    Check("Client cannot extend server TTL to 30 minutes", reclaimBody.GetProperty("data").GetProperty("remainingSeconds").GetInt32() <= 600);
    DateTime heldAt;
    await using (var db = database.OpenContext()) heldAt = (await db.Tickets.Include(t => t.Booking).SingleAsync(t => t.Booking.TripId == database.Ids.Trip
        && t.Booking.UserId == database.Ids.CustomerB && t.SeatNumber == "A03" && t.Status == "HELD")).Booking.BookingTime;
    await b.PostAsJsonAsync("/api/bookings/hold-seats", new { tripId = database.Ids.Trip, seatNumbers = new[] { "A03" } });
    await using (var db = database.OpenContext())
    {
        var active = await db.Tickets.Include(t => t.Booking).Where(t => t.Booking.TripId == database.Ids.Trip && t.SeatNumber == "A03"
            && t.Status == "HELD" && t.Booking.BookingTime > DateTime.Now.AddMinutes(-10)).ToListAsync();
        Check("Reallocation/retry has one active allocation", active.Count == 1);
        Check("Retry preserves original hold timestamp", active.Count == 1 && active[0].Booking.BookingTime == heldAt);
        Check("Returned quote equals server ticket sum", active.Count == 1 && active[0].Booking.TotalAmount
            == reclaimBody.GetProperty("data").GetProperty("totalPrice").GetDecimal());
    }
    var freeMap = await (await freshNode.GetAsync("/api/trips/" + database.Ids.Trip + "/seats")).Content.ReadFromJsonAsync<JsonElement>();
    var freeSeats = freeMap.GetProperty("data").GetProperty("seats").EnumerateArray().Where(seat => seat.GetProperty("status").GetString() == "AVAILABLE")
        .Select(seat => seat.GetProperty("seatNumber").GetString()!).Take(7).ToArray();
    Check("Six-seat hold accepted", freeSeats.Length == 7 && (int)(await a.PostAsJsonAsync("/api/bookings/hold-seats",
        new { tripId = database.Ids.Trip, seatNumbers = freeSeats.Take(6).ToArray() })).StatusCode == 200);
    Check("Seventh cumulative seat rejected", (int)(await a.PostAsJsonAsync("/api/bookings/hold-seats",
        new { tripId = database.Ids.Trip, seatNumbers = new[] { freeSeats[6] } })).StatusCode == 400);
    var absent = new SqlConnectionStringBuilder(database.ConnectionString) { InitialCatalog = database.Name + "absent", ConnectTimeout = 1 };
    var outagePort = FreePort(); Launch(product, new[] { "--urls", $"http://127.0.0.1:{outagePort}" }, absent.ConnectionString);
    using var outage = Client(outagePort); await WaitForServer(outage);
    Check("DB failure plus demo password cannot create login", (int)(await Login(outage, "g0.customer.a@example.invalid", "123456")).StatusCode == 200
        && (int)(await outage.GetAsync("/Ticket/Detail?id=" + database.Ids.PendingTicketCode)).StatusCode == 302);
    await using (var db = database.OpenContext()) { var u = await db.Users.FindAsync(database.Ids.CustomerA); u!.Status = "Blocked"; await db.SaveChangesAsync(); }
    Check("Existing cookie rejected after account blocked", (int)(await a.GetAsync("/Ticket/Detail?id=" + database.Ids.PendingTicketCode)).StatusCode == 302);
    await using (var db = database.OpenContext())
    { var principal = await WebApplication1.BackendIdentity.LoadAsync(db, database.Ids.DriverUser); Check("Revalidated driver principal has no Admin role", principal != null && !principal.IsInRole("Admin") && !principal.IsInRole("Quản trị viên")); }
}
catch (Exception e)
{ Check("Harness completed", false, new { type = e.GetType().Name, error = e.Message }); }
finally
{
    foreach (var process in processes) { if (!process.HasExited) process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); process.Dispose(); }
    await database.DropOwnedEphemeralAsync();
    Directory.CreateDirectory(resultsDir);
    await File.WriteAllTextAsync(Path.Combine(resultsDir, "backend.json"), JsonSerializer.Serialize(new
    { generatedAtUtc = DateTimeOffset.UtcNow, sourceBase = "ba98ee9", database = database.Name, databaseDropped = true,
        passed = results.Count - failures, failed = failures, tests = results }, new JsonSerializerOptions { WriteIndented = true }));
    await File.WriteAllLinesAsync(Path.Combine(resultsDir, "host.log"), logs);
}
Console.WriteLine($"RESULT: {results.Count - failures}/{results.Count} passed; failures={failures}; test DB cleaned");
Environment.ExitCode = failures == 0 ? 0 : 1;
