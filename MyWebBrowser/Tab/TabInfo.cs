using Microsoft.Web.WebView2.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;

public class TabData : INotifyPropertyChanged
{
    private string _url;
    private bool _isLoading;
    public WebView2 WebView2 { get; set; }

    public ObservableCollection<string> History { get; set; } = new ObservableCollection<string>();

    public string Url
    {
        get => _url;
        set
        {
            if (_url != value)
            {
                _url = value;
                OnPropertyChanged(nameof(Url));
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(ReloadIconPath));
            }
        }
    }

    public string ReloadIconPath => IsLoading ? "/Icon/close.png" : "/Icon/refresh.png";

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class TabInfo : INotifyPropertyChanged
{
    private string _header;
    private TabData _tabData;

    public string Header
    {
        get => _header;
        set
        {
            if (_header != value)
            {
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }
    }

    public TabData TabData
    {
        get => _tabData;
        set
        {
            if (_tabData != value)
            {
                _tabData = value;
                OnPropertyChanged(nameof(TabData));
            }
        }
    }

    public TabInfo() { TabData = new TabData(); }

    public override string ToString() => Header ?? base.ToString();

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public bool IsIncognito { get; set; }
    public string IncognitoProfilePath { get; set; } // Đường dẫn profile tạm của tab ẩn danh
    public WebView2 Content { get; set; }
}