using System;
using System.IO;
using System.Text.Json;

using ParcelManager.Models;

namespace ParcelManager.Services
{
    public class ConfigService
    {
        private readonly string _configPath;

        public AppConfig Config { get; private set; }

        public ConfigService()
        {
            string appFolder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "TopMap",
                    "LandParcelManager");

            Directory.CreateDirectory(appFolder);

            _configPath =
                Path.Combine(appFolder, "config.json");

            Config = Load();
        }

        public void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json =
                JsonSerializer.Serialize(
                    Config,
                    options);

            File.WriteAllText(
                _configPath,
                json);
        }

        private AppConfig Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    return new AppConfig();
                }

                string json =
                    File.ReadAllText(_configPath);

                var config =
                    JsonSerializer.Deserialize<AppConfig>(
                        json);

                return config ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }
    }
}