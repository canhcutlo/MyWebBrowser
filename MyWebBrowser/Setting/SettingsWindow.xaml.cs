using MyWebBrowser;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace MyWebBrowser.Setting
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        private readonly string _settingsPath;
        private bool _isInitialized = false; // guard để tránh xử lý quá sớm

        public SettingsWindow()
        {
            InitializeComponent();

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "MyWebBrowserVer4");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _settingsPath = Path.Combine(folder, "settings.json");

            _settings = SettingsManager.Load(_settingsPath);

            // populate UI
            DownloadFolderTextBox.Text = _settings.DefaultDownloadFolder ?? "";
            AskEachTimeCheckBox.IsChecked = _settings.AskEachTimeBeforeDownload;

            if (string.Equals(_settings.Theme, "Dark", StringComparison.OrdinalIgnoreCase))
                ThemeDarkRadio.IsChecked = true;
            else
                ThemeLightRadio.IsChecked = true;

            // Tạm tháo event để tránh ValueChanged nổ trong lúc khởi tạo
            ZoomSlider.ValueChanged -= ZoomSlider_ValueChanged;
            ZoomSlider.Value = _settings.DefaultZoomPercent > 0 ? _settings.DefaultZoomPercent : 100;
            ZoomPercentText.Text = $"{(int)ZoomSlider.Value}%";
            ZoomSlider.ValueChanged += ZoomSlider_ValueChanged;

            // Đánh dấu đã khởi tạo xong
            _isInitialized = true;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select default download folder",
                UseDescriptionForTitle = true,
                SelectedPath = string.IsNullOrWhiteSpace(DownloadFolderTextBox.Text)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    : DownloadFolderTextBox.Text
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DownloadFolderTextBox.Text = dlg.SelectedPath;
            }
        }

        private void ZoomInBtn_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value = Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + 10);
        }

        private void ZoomOutBtn_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, ZoomSlider.Value - 10);
        }

        private void ZoomSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized) return; // đảm bảo UI đã sẵn sàng
            // Dùng e.NewValue để chính xác hơn, tránh đọc trực tiếp từ control
            ZoomPercentText.Text = $"{(int)e.NewValue}%";
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            _settings.DefaultDownloadFolder = string.IsNullOrWhiteSpace(DownloadFolderTextBox.Text) ? null : DownloadFolderTextBox.Text;
            _settings.AskEachTimeBeforeDownload = AskEachTimeCheckBox.IsChecked == true;
            _settings.Theme = ThemeDarkRadio.IsChecked == true ? "Dark" : "Light";
            _settings.DefaultZoomPercent = (int)ZoomSlider.Value;

            SettingsManager.Save(_settingsPath, _settings);

            if (this.Owner is ISettingsReceiver receiver)
            {
                try
                {
                    receiver.ApplySettings(_settings);
                }
                catch
                {
                    // ignore apply failure but still close/save
                }
            }

            this.DialogResult = true;
            this.Close();
        }
    }


    public class AppSettings
    {
        public string DefaultDownloadFolder { get; set; }
        public bool AskEachTimeBeforeDownload { get; set; } = false;
        public string Theme { get; set; } = "Light";
        public int DefaultZoomPercent { get; set; } = 100;
    }


    public static class SettingsManager
    {
        public static AppSettings Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var s = JsonSerializer.Deserialize<AppSettings>(json);
                    if (s != null) return s;
                }
            }
            catch
            { }
            return new AppSettings();
        }

        public static void Save(string path, AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi khi lưu cài đặt: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}