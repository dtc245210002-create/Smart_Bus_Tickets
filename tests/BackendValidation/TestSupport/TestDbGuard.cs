using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SmartBus.G0.TestSupport;

/// <summary>Deliberately rejects TCP, Somee, default LocalDB and any non-test database, before opening a socket.</summary>
public static partial class TestDbGuard
{
    public const string Instance = @"(localdb)\SmartBusG0";
    public const string Prefix = "SmartBus_G0_Test_";
    public const string Purpose = "smartbus-g0-isolated-test";
    public const string SeedVersion = "legacy-baseline-v1";
    public const string ContractVersion = "1.0.0";

    [GeneratedRegex(@"^SmartBus_G0_Test_[A-Za-z0-9_]{1,80}$", RegexOptions.CultureInvariant)]
    private static partial Regex DatabasePattern();

    public static string ConnectionString(string database)
    {
        if (!DatabasePattern().IsMatch(database)) throw new ArgumentException("G0_DATABASE_NAME_REJECTED");
        var result = new SqlConnectionStringBuilder
        {
            DataSource = Instance, InitialCatalog = database, IntegratedSecurity = true,
            Encrypt = false, ConnectTimeout = 15, ConnectRetryCount = 0,
            ApplicationName = "SmartBus.G0.IsolatedTests", MultipleActiveResultSets = false
        }.ConnectionString;
        Validate(result);
        return result;
    }

    public static void Validate(string connectionString)
    {
        var cs = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(cs.DataSource, Instance, StringComparison.OrdinalIgnoreCase)
            || !DatabasePattern().IsMatch(cs.InitialCatalog)
            || !cs.IntegratedSecurity || !string.IsNullOrEmpty(cs.UserID)
            || !string.IsNullOrEmpty(cs.Password) || !string.IsNullOrEmpty(cs.AttachDBFilename)
            || !string.IsNullOrEmpty(cs.FailoverPartner))
            throw new ArgumentException("G0_TARGET_REJECTED: only the dedicated Windows LocalDB instance and G0-prefixed catalog are allowed.");
    }

    // Only internal provisioning may use master, to check/create/drop our guarded test catalog.
    internal static string MasterConnection(string guardedConnection)
    {
        Validate(guardedConnection);
        return new SqlConnectionStringBuilder(guardedConnection) { InitialCatalog = "master" }.ConnectionString;
    }
}
