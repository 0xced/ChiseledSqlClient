using System.Runtime.CompilerServices;
using ChiseledDeps;
using NuGet.ProjectModel;

var depsPath = GetDepsPath();
string[] packagesToRemove = [
    "Azure.Identity",
    "Microsoft.Identity.Client",
    "Microsoft.IdentityModel.JsonWebTokens",
    "Microsoft.IdentityModel.Protocols.OpenIdConnect",
    "System.Configuration.ConfigurationManager"
];

var lockFile = new LockFileFormat().Read(depsPath);
var graph = new DependencyGraph(lockFile);
var removed = graph.Remove(packagesToRemove);
await graph.WriteAsync(Console.OpenStandardOutput(), GraphFormat.Svg);

Console.Error.WriteLine("<ItemGroup>");
foreach (var package in removed)
{
    Console.Error.WriteLine($"  <PackageReference Include=\"{package.Name}\" Version=\"{package.Version}\" ExcludeAssets=\"all\" />");
}
Console.Error.WriteLine("</ItemGroup>");

return;

static string GetDepsPath([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "ChiseledSqlClient", "obj", "project.assets.json"));
