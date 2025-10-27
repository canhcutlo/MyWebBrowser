using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyWebBrowser
{
    public partial class HistoryPopup : Window
    {
        public ObservableCollection<HistoryItem> CurrentTabHistory { get; set; } = new ObservableCollection<HistoryItem>();
        public ObservableCollection<HistoryItem> GlobalHistory { get; set; } = new ObservableCollection<HistoryItem>();

        public string SelectedUrl { get; private set; } = null;

        private bool _isClosing = false;
        public bool IsClosing => _isClosing;
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;
            base.OnClosing(e);
        }
        public HistoryPopup()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is HistoryItem item)
            {
                SelectedUrl = item.Url;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void GlobalHistoryItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is HistoryItem item)
            {
                SelectedUrl = item.Url;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void DeleteCurrentTabHistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is HistoryItem item)
            {
                CurrentTabHistory.Remove(item);
            }
        }

        private void DeleteGlobalHistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is HistoryItem item)
            {
                GlobalHistory.Remove(item);
            }
        }
        private void DeleteAllGlobalHistory_Click(object sender, RoutedEventArgs e)
        {
            System.Windows. MessageBox.Show("Bạn chắc chắn muốn xóa toàn bộ lịch sử chung?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (System.Windows.MessageBox.Show("", "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                GlobalHistory.Clear();
                if (MyWebBrowser.WebWindow.historyManager != null)
                {
                    MyWebBrowser.WebWindow.historyManager.GlobalHistory.Clear();
                    MyWebBrowser.WebWindow.historyManager.SaveHistory();
                }
            }
        }
        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!this.IsClosing)
            {
                this.Close();
            }
        }
    }
}