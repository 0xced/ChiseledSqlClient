using System.Collections;

namespace System.Configuration
{
    public class ConfigurationElementCollection : IEnumerable
    {
        public int Count => 0;

        public IEnumerator GetEnumerator()
        {
            yield break;
        }
    }

    public class ConfigurationErrorsException
    {
    }

    public class ConfigurationManager
    {
        public static object GetSection(string sectionName) => null;
    }

    public class ConfigurationSection
    {
    }

    public class ProviderSettings
    {
        public string Name => null;
        public string Type => null;
    }

    public class ProviderSettingsCollection
    {
    }
}
