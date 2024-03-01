using System.Diagnostics;

namespace ChiseledDeps;

[DebuggerDisplay("{Name}/{Version}")]
internal class Package : IEquatable<Package>
{
    public Package(string name, string version, bool hasDll, IReadOnlyCollection<string> dependencies)
    {
        Name = name;
        Version = version;
        HasDll = hasDll;
        Dependencies = dependencies;
    }

    public string Name { get; }
    public string Version { get; }
    public bool HasDll { get; }
    public IReadOnlyCollection<string> Dependencies { get; }

    public string Id => $"{Name}/{Version}";

    public bool Keep { get; set; } = true;

    public override string ToString() => Name;

    public bool Equals(Package? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Package)obj);
    }

    public override int GetHashCode() => Name.GetHashCode();
}