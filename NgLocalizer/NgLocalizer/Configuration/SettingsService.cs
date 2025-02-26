using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace NgLocalizer.Configuration
{
    internal class SettingsService
    {
        public void Load()
        {
            var file = GetConfigFilename();
            if (!File.Exists(file))
            {
                MessageBox.Show("There is no Angular project linked to NgLocalizer yet. Do this first: via the \"Config\" menu.", "First startup", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var configs = File.ReadAllText(file).Split('\r');
            SourceFolder = configs[0].Trim();
            I18NFolder = configs[1].Trim();
        }

        public async Task SaveAsync()
        {
            await File.WriteAllTextAsync(GetConfigFilename(), SourceFolder + Environment.NewLine + I18NFolder);
        }

        public string I18NFolder { get; set; }

        public string SourceFolder { get; set; }

        private static string GetConfigFilename()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NgLocalizer.cfg");
        }
    }
}
