using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace MyWebBrowser
{
    public partial class BookmarkEditorPopup : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Bookmark Bookmark { get; private set; }
        public bool IsDeleted { get; private set; } = false;

        private string _iconUrl;
        public string IconUrl
        {
            get => _iconUrl;
            set
            {
                _iconUrl = value;
                OnPropertyChanged(nameof(IconUrl));
            }
        }

        public BookmarkEditorPopup(Bookmark bookmark)
        {
            InitializeComponent();
            this.Bookmark = bookmark;
            this.IconUrl = bookmark.IconUrl; // Giả sử Bookmark có thuộc tính IconUrl
            this.DataContext = this;
        }

        private void DoneBtn_Click(object sender, RoutedEventArgs e)
        {
            Bookmark.Title = TitleBox.Text;
            this.DialogResult = true;
            this.Close();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            IsDeleted = true;
            this.DialogResult = true;
            this.Close();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}