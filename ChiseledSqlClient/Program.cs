using Microsoft.Data.SqlClient;

var dlls = new DirectoryInfo(AppContext.BaseDirectory).EnumerateFiles("*.dll").Where(e => e.Name != "ChiseledSqlClient.dll").OrderByDescending(e => e.Length).ToList();
var isChiseled = dlls.All(e => !e.Name.Contains("Azure"));
var maxLength = dlls.Max(e => e.Name.Length) + (isChiseled ? 0 : 4);
if (args.Contains("--dlls"))
{
    Console.WriteLine($"| File {new string(' ', maxLength - 4)}| Size    |");
    Console.WriteLine($"|---{new string('-', maxLength - 4)}---|---------|");
    foreach (var dll in dlls)
    {
        var name = !isChiseled && (dll.Name.StartsWith("Azure") || dll.Name.StartsWith("Microsoft.Identity") || dll.Name.Contains("msal")) ? $"**{dll.Name}**" : dll.Name;
        Console.WriteLine($"| {name.PadRight(maxLength)} | {dll.Length / 1_000_000.0:F2} MB |");
    }
}

var totalDllSize = dlls.Sum(e => e.Length);
Console.WriteLine($"Total DLL size: {totalDllSize / 1_000_000.0:F1} MB");

var connectionString = args.Length > 0 && !args[^1].StartsWith("--") ? args[^1] : "Server=sqlprosample.database.windows.net;Database=sqlprosample;user=sqlproro;password=nh{Zd?*8ZU@Y}Bb#";
await using var dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);
await using var command = dataSource.CreateCommand("Select @@version");
try
{
    var result = await command.ExecuteScalarAsync();
    Console.WriteLine($"✅ {result}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"❌ {exception}");
    return 1;
}
