using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Text.Json;
namespace MyWebBrowser
{
    public partial class DownloadedPopup : Window
    {
        public ObservableCollection<DownloadedItem> DownloadHistory { get; set; }

        public DownloadedPopup(ObservableCollection<DownloadedItem> history)
        {
            InitializeComponent();
            DownloadHistory = history;
            this.DataContext = this;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var path = btn?.Tag as string;
            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Không mở được file:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("File không tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

    }
}