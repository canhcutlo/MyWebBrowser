using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyWebBrowser
{
    public partial class BookmarkPopup : Window
    {
        public ObservableCollection<Bookmark> Bookmarks { get; set; } = new ObservableCollection<Bookmark>();
        public string SelectedUrl { get; private set; } = null;

        public BookmarkPopup()
        {
            InitializeComponent();
            DataContext = this;
        }
        private bool _isClosing = false;
        public bool IsClosing => _isClosing;
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;
            base.OnClosing(e);
        }

        private void BookmarkItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Bookmark item)
            {
                SelectedUrl = item.Url;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void DeleteBookmarkItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Bookmark item)
            {
                Bookmarks.Remove(item);
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