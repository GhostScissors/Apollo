using Apollo.ViewModels;
using Newtonsoft.Json;

namespace Apollo.Settings;

public class AppSettings
{
    public static SettingsViewModel Current = new();

    private static readonly DirectoryInfo SettingsDirectory = new(Path.Combine(Environment.CurrentDirectory, "Output", "Settings"));
    private static readonly FileInfo FilePath = new(Path.Combine(SettingsDirectory.FullName, "AppSettings.json"));

    public static void Load()
    {
        if (!SettingsDirectory.Exists) SettingsDirectory.Create();
        if (!FilePath.Exists) return;
        Current = JsonConvert.DeserializeObject<SettingsViewModel>(File.ReadAllText(FilePath.FullName)) ?? new SettingsViewModel();
    }

    public static void Save()
    {
        File.WriteAllText(FilePath.FullName, JsonConvert.SerializeObject(Current, Formatting.Indented));
    }
}