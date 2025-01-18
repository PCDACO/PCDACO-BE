using Newtonsoft.Json;

namespace API.Utils;

public static class ConfigurationServices
{
    public static IConfigurationSection ObjectToConfigurationSection<T>(T obj)
    {
        // Serialize the object to JSON
        var json = JsonConvert.SerializeObject(obj);

        // Load the JSON into a MemoryStream
        var memoryStream = new MemoryStream();
        var writer = new StreamWriter(memoryStream);
        writer.Write(json);
        writer.Flush();
        memoryStream.Position = 0;

        // Build a configuration from the MemoryStream
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(memoryStream)
            .Build();

        // Return the root section (or a subsection if needed)
        return configuration.GetSection("");
    }
}