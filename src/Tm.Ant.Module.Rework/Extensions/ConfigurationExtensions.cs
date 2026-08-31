using System.IO;
using Newtonsoft.Json;
using Tm.Ant.Module.Rework.Models;

namespace Tm.Ant.Module.Rework.Extensions;
internal static class ConfigurationExtensions
{
    private const string FILENAME = "ReworkOption";
    public static ReworkOption GetReworkOption(this IConfiguration configuration)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", $"appsettings.{FILENAME}.json");
        if (File.Exists(filePath))
        {
            var text=File.ReadAllText(filePath);
            var config = JsonConvert.DeserializeObject<ReworkOption>(text);
            return config;
        }
        return new ReworkOption();
    }
     
    public static void SaveReworkOption(this IConfiguration configuration, Action<ReworkOption> change)
    {
        var config = configuration.GetReworkOption();
        change(config); 
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", $"appsettings.{FILENAME}.json");
        File.WriteAllText(filePath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
}
