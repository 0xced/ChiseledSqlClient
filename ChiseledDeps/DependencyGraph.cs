using System.Drawing;
using CliWrap;
using GiGraph.Dot.Entities.Edges;
using GiGraph.Dot.Entities.Graphs;
using GiGraph.Dot.Extensions;
using GiGraph.Dot.Types.Nodes;
using NuGet.ProjectModel;

namespace ChiseledDeps;

public enum GraphFormat
{
    Dot,
    Svg,
}

public class DependencyGraph
{
    private readonly HashSet<Package> _roots;
    private readonly Dictionary<Package, HashSet<Package>> _graph = new();
    private readonly Dictionary<Package, HashSet<Package>> _reverseGraph = new();

    private static Package CreatePackage(LockFileTargetLibrary library)
    {
        var name = library.Name ?? throw new ArgumentException("The library must have a name", nameof(library));
        var version = library.Version?.ToString() ?? throw new ArgumentException("The library must have a version", nameof(library));
        var hasDll = library.RuntimeAssemblies.Any(e => e.Path.EndsWith(".dll"));
        var dependencies = library.Dependencies.Select(e => e.Id).ToList();
        return new Package(name, version, hasDll, dependencies);
    }

    public DependencyGraph(LockFile assetsLockFile)
    {
        var targetFramework = assetsLockFile.PackageSpec.TargetFrameworks.OrderByDescending(e => e.FrameworkName).First();
        var target = assetsLockFile.Targets.Single(e => e.TargetFramework == targetFramework.FrameworkName && e.RuntimeIdentifier == null);
        var packages = target.Libraries.ToDictionary(e => e.Name ?? "", CreatePackage);

        _roots = targetFramework.Dependencies.Select(e => packages[e.Name]).ToHashSet();

        foreach (var package in packages.Values.Where(e => e.HasDll))
        {
            var dependencies = package.Dependencies.Select(e => packages[e]).Where(e => e.HasDll).ToHashSet();

            if (dependencies.Count > 0)
            {
                _graph.Add(package, dependencies);
            }

            foreach (var dependency in dependencies)
            {
                if (_reverseGraph.TryGetValue(dependency, out var reverseDependencies))
                {
                    reverseDependencies.Add(package);
                }
                else
                {
                    _reverseGraph[dependency] = [package];
                }
            }
        }
    }

    public IReadOnlyCollection<(string Name, string Version)> Remove(IEnumerable<string> packages)
    {
        var notFound = new List<string>();
        var dependencies = new HashSet<Package>();
        foreach (var packageName in packages)
        {
            var packageDependency = _reverseGraph.Keys.SingleOrDefault(e => e.Name == packageName);
            if (packageDependency == null)
            {
                notFound.Add(packageName);
            }
            else
            {
                dependencies.Add(packageDependency);
            }
        }

        _ = notFound.Count switch
        {
            0 => 0,
            1 => throw new ArgumentException($"\"{notFound[0]}\" was not found in the dependency graph.", nameof(packages)),
            2 => throw new ArgumentException($"\"{notFound[0]}\" and \"{notFound[1]}\" were not found in the dependency graph.", nameof(packages)),
            _ => throw new ArgumentException($"{string.Join(", ", notFound.Take(notFound.Count - 1).Select(e => $"\"{e}\""))} and \"{notFound.Last()}\" were not found in the dependency graph.", nameof(packages)),
        };

        foreach (var dependency in dependencies)
        {
            Remove(dependency);
            Restore(dependency, dependencies);
        }

        return _reverseGraph.Keys.Where(e => !e.Keep).Select(e => (e.Name, e.Version)).OrderBy(e => e.Name).ToList();
    }

    private void Remove(Package package)
    {
        package.Keep = false;
        if (_graph.TryGetValue(package, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                Remove(dependency);
            }
        }
    }

    private void Restore(Package package, IReadOnlySet<Package> removedPackages)
    {
        if ((_reverseGraph[package].Any(e => e.Keep) && !removedPackages.Contains(package)) || _roots.Contains(package))
        {
            package.Keep = true;
        }

        if (_graph.TryGetValue(package, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                Restore(dependency, removedPackages);
            }
        }
    }

    public async Task WriteAsync(Stream output, GraphFormat format)
    {
        var dotGraph = new DotGraph
        {
            Nodes =
            {
                Shape = DotNodeShape.Box,
                Style = { FillStyle = DotNodeFillStyle.Normal },
                Font = { Name = "Segoe UI, sans-serif" },
            },
        };

        foreach (var package in _reverseGraph.Keys.Union(_roots).OrderBy(e => e.Name))
        {
            dotGraph.Nodes.Add(package.Id, node =>
            {
                node.Color = package.HasDll ? package.Keep ? Color.Aquamarine : Color.LightCoral : Color.LightGray;
                node.Attributes.Collection.Set("URL", $"https://www.nuget.org/packages/{package.Name}/{package.Version}");
                node.Attributes.Collection.Set("target", "_blank");
            });
        }

        foreach (var (package, dependencies) in _graph)
        {
            foreach (var dependency in dependencies)
            {
                dotGraph.Edges.Add(new DotEdge(package.Id, dependency.Id));
            }
        }

        void WriteGraph(Stream stream)
        {
            using var writer = new StreamWriter(stream);
            dotGraph.Build(writer);
        }

        if (format == GraphFormat.Dot)
        {
            WriteGraph(output);
            return;
        }

        var input = PipeSource.Create(WriteGraph);

        var dot = Cli.Wrap("dot")
            .WithArguments([ $"-T{format.ToString().ToLowerInvariant()}" ])
            .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.Error.WriteLine));

        await (input | dot | output).ExecuteAsync();
    }
}
