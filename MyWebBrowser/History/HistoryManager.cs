using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MyWebBrowser
{
    public class HistoryManager
    {
        private string historyFile = "history.json";
        public List<HistoryItem> GlobalHistory { get; private set; } = new List<HistoryItem>();

        public HistoryManager()
        {
            LoadHistory();
        }

        public void AddHistory(HistoryItem item)
        {
            if (GlobalHistory.Count == 0 || GlobalHistory[GlobalHistory.Count - 1].Url != item.Url)
            {
                GlobalHistory.Add(item);
                SaveHistory();
            }
        }

        public void RemoveHistory(HistoryItem item)
        {
            GlobalHistory.Remove(item);
            SaveHistory();
        }

        public void SaveHistory()
        {
            File.WriteAllText(historyFile, JsonSerializer.Serialize(GlobalHistory));
        }

        public void LoadHistory()
        {
            if (File.Exists(historyFile))
            {
                GlobalHistory = JsonSerializer.Deserialize<List<HistoryItem>>(File.ReadAllText(historyFile)) ?? new List<HistoryItem>();
            }
        }
    }
}