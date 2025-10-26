using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
public class DownloadedItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; } // bytes
        public DateTime DownloadedAt { get; set; }
        public string IconUrl { get; set; } // icon cho loại file (png, xaml, ...)
    }

namespace MyWebBrowser
{
    public static class DownloadHistoryManager
    {
        public static void SaveDownloadHistory(ObservableCollection<DownloadedItem> history, string filePath)
        {
            var json = JsonSerializer.Serialize(history);
            File.WriteAllText(filePath, json);
        }

        public static ObservableCollection<DownloadedItem> LoadDownloadHistory(string filePath)
        {
            if (!File.Exists(filePath)) return new ObservableCollection<DownloadedItem>();
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ObservableCollection<DownloadedItem>>(json)
                ?? new ObservableCollection<DownloadedItem>();
        }
    }
}