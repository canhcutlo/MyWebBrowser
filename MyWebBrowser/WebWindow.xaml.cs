using Dragablz;
using Dragablz.Dockablz;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using MyWebBrowser.Setting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static MyWebBrowser.WebWindow;


namespace MyWebBrowser
{

    public class TabInfo : INotifyPropertyChanged
    {
        private string _header;
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
        public WebView2 Content { get; set; }
        public TabData TabData { get; set; }
        public bool IsIncognito { get; set; }
        public string IncognitoProfilePath { get; set; } // Đường dẫn profile tạm của tab ẩn danh

        public override string ToString() => Header ?? base.ToString();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
    public interface ISettingsReceiver
    {
        void ApplySettings(MyWebBrowser.Setting.AppSettings settings);
    }

    public partial class WebWindow : Window, ISettingsReceiver
    {
        private bool isSummarizingAI = false;
        private bool isChoosingSuggestion = false;
        private TabablzControl browserTabs;

        public static BookmarkManager bookmarkManager;
        public static HistoryManager historyManager;

        private bool isDraggingTab = false;

        public ObservableCollection<HistoryItem> RecentHistory { get; set; } = new ObservableCollection<HistoryItem>();
        public ObservableCollection<TabInfo> Tabs { get; } = new ObservableCollection<TabInfo>();

        public static ObservableCollection<HistoryItem> allHistoryList = new ObservableCollection<HistoryItem>();

        ObservableCollection<DownloadedItem> HistoryDownloaded;

        public ObservableCollection<string> Suggestions { get; set; } = new ObservableCollection<string>();

        const string HistoryFilePath = "download_history.json";

        public static RoutedCommand ShowHistoryCommand = new RoutedCommand();
        public static RoutedCommand ShowBookmarkCommand = new RoutedCommand();
        public static RoutedCommand ShowDownloadHistoryCommand = new RoutedCommand();
        public static RoutedCommand SummarizeAICommand = new RoutedCommand();
        public static RoutedCommand NewIncognitoTabCommand = new RoutedCommand();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TabInfo SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged(nameof(SelectedTab));
            }
        }
        private TabInfo _selectedTab;

        public WebWindow()
        {
            InitializeComponent();
            DataContext = this;

            historyManager = new HistoryManager();
            bookmarkManager = new BookmarkManager();

            browserTabs = BrowserTabs; // Ensure browserTabs is initialized
            HistoryDownloaded = DownloadHistoryManager.LoadDownloadHistory(HistoryFilePath);

            browserTabs.PreviewMouseLeftButtonDown += (s, e) => { isDraggingTab = false; };
            browserTabs.PreviewMouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    isDraggingTab = true;
            };
            browserTabs.Drop += (s, e) => { isDraggingTab = false; };

            // Initialize a WebView2 instance to attach download event handler when CoreWebView2 initialized
            var webView2 = new WebView2();
            webView2.CoreWebView2InitializationCompleted += (s, e) =>
            {
                if (e.IsSuccess)
                {
                    try
                    {
                        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        var folder = Path.Combine(appData, "MyWebBrowserVer4");
                        var settingsPath = Path.Combine(folder, "settings.json");
                        var settings = SettingsManager.Load(settingsPath); // SettingsManager ở namespace MyWebBrowser.Setting
                        webView2.ZoomFactor = settings.DefaultZoomPercent / 100.0;
                    }
                    catch { }
                    webView2.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
                }
            };

            browserTabs.NewItemFactory = () => CreateNewTab();
            browserTabs.SelectionChanged += BrowserTabs_SelectionChanged;
            browserTabs.InterTabController.Partition = "MainPartition";

            CommandBindings.Add(new CommandBinding(ShowHistoryCommand, HistoryBtn_Executed));
            CommandBindings.Add(new CommandBinding(ShowBookmarkCommand, SavedBookmark_Executed));
            CommandBindings.Add(new CommandBinding(ShowDownloadHistoryCommand, ShowDownloadHistory_Executed));
            CommandBindings.Add(new CommandBinding(SummarizeAICommand, SummarizeAI_Executed));
            CommandBindings.Add(new CommandBinding(NewIncognitoTabCommand, IncognitoBtn_Click));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateToolbarForCurrentTab();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private TabInfo CreateNewTab()
        {
            var webView2 = new WebView2
            {
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
            };
            webView2.CoreWebView2InitializationCompleted += (s, e) =>
            {
                if (e.IsSuccess)
                {
                    webView2.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                    webView2.CoreWebView2.NavigationCompleted += (sender2, args2) =>
                    {
                        var tab = FindTabOfWebView2(webView2);
                        if (tab != null)
                        {
                            var url = webView2.Source?.ToString();

                            // Nếu tab đang được chọn và người dùng đang sửa URL thì không ghi đè
                            bool isSelected = BrowserTabs.SelectedItem == tab;
                            if (!tab.TabData.IsEditingUrl)
                            {
                                tab.TabData.Url = url;
                            }

                            if (tab.IsIncognito)
                                tab.Header = "Ẩn danh";
                            else
                                tab.Header = GetTabHeaderFromUrl(url);

                            tab.TabData.IsLoading = false;

                            // Thêm vào lịch sử (không thay đổi)
                            if (!tab.IsIncognito && !string.IsNullOrWhiteSpace(url))
                            {
                                if (tab.TabData.History.Count == 0 || tab.TabData.History.Last() != url)
                                    tab.TabData.History.Add(url);

                                if (allHistoryList.Count == 0 || allHistoryList.Last().Url != url)
                                {
                                    allHistoryList.Add(new HistoryItem
                                    {
                                        Title = GetTabHeaderFromUrl(url),
                                        Description = url,
                                        IconUrl = GetFaviconUrl(url),
                                        Url = url
                                    });
                                }
                                historyManager.AddHistory(new HistoryItem
                                {
                                    Title = GetTabHeaderFromUrl(url),
                                    Description = url,
                                    IconUrl = GetFaviconUrl(url),
                                    Url = url
                                });
                            }
                        }
                    };
                    webView2.CoreWebView2.NavigationStarting += (sender2, args2) =>
                    {
                        var tab = FindTabOfWebView2(webView2);
                        if (tab != null)
                        {
                            tab.TabData.IsLoading = true;
                        }
                    };
                    webView2.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
                }
            };

            // Khởi tạo WebView2 (bắt buộc trước khi dùng CoreWebView2)
            webView2.Source = new Uri("https://www.google.com");

            var tabData = new TabData
            {
                WebView2 = webView2,
                IsLoading = false
            };

            var tabInfo = new TabInfo
            {
                Header = "Loading...",
                Content = webView2,
                TabData = tabData
            };

            return tabInfo;
        }

        public class IncognitoIconConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool isIncognito = value is bool b && b;
                return isIncognito ? "/Icon/Incognito.png" : "/Icon/Webbrowser.png";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public async Task<TabInfo> CreateIncognitoTabAsync()
        {
            try
            {
                string tempProfile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                var env = await CoreWebView2Environment.CreateAsync(null, tempProfile);

                var webView2 = new WebView2();
                await webView2.EnsureCoreWebView2Async(env);

                webView2.CoreWebView2.NavigationCompleted += (sender2, args2) =>
                {
                    var tab = FindTabOfWebView2(webView2);
                    if (tab != null)
                    {
                        var url = webView2.Source?.ToString();

                        // Nếu tab đang được chọn và người dùng đang sửa URL thì không ghi đè
                        bool isSelected = BrowserTabs.SelectedItem == tab;
                        if (!tab.TabData.IsEditingUrl)
                        {
                            tab.TabData.Url = url;
                        }

                        if (tab.IsIncognito)
                            tab.Header = "Ẩn danh";
                        else
                            tab.Header = GetTabHeaderFromUrl(url);

                        tab.TabData.IsLoading = false;

                        // Thêm vào lịch sử (không thay đổi)
                        if (!tab.IsIncognito && !string.IsNullOrWhiteSpace(url))
                        {
                            if (tab.TabData.History.Count == 0 || tab.TabData.History.Last() != url)
                                tab.TabData.History.Add(url);

                            if (allHistoryList.Count == 0 || allHistoryList.Last().Url != url)
                            {
                                allHistoryList.Add(new HistoryItem
                                {
                                    Title = GetTabHeaderFromUrl(url),
                                    Description = url,
                                    IconUrl = GetFaviconUrl(url),
                                    Url = url
                                });
                            }
                            historyManager.AddHistory(new HistoryItem
                            {
                                Title = GetTabHeaderFromUrl(url),
                                Description = url,
                                IconUrl = GetFaviconUrl(url),
                                Url = url
                            });
                        }
                    }
                };
                webView2.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                webView2.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

                var tabData = new TabData
                {
                    WebView2 = webView2,
                    IsLoading = false
                };

                var tabInfo = new TabInfo
                {
                    Header = "Ẩn danh",
                    Content = webView2,
                    TabData = tabData,
                    IsIncognito = true,
                    IncognitoProfilePath = tempProfile
                };

                webView2.Source = new Uri("https://www.google.com");
                return tabInfo;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi khởi tạo Incognito: " + ex.Message);
                return null;
            }
        }

        private string GetTabHeaderFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "New Tab";
            try
            {
                var uri = new Uri(url);
                return uri.Host.Replace("www.", "");
            }
            catch
            {
                return url;
            }
        }

        private async void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            var currentTab = GetCurrentTabInfo();
            TabInfo newTab;
            if (currentTab != null && currentTab.IsIncognito)
                newTab = await CreateIncognitoTabAsync();
            else
                newTab = CreateNewTab();

            Tabs.Add(newTab);
            BrowserTabs.SelectedItem = newTab;

            if (newTab.TabData.WebView2 != null && Uri.IsWellFormedUriString(e.Uri, UriKind.Absolute))
                newTab.TabData.WebView2.Source = new Uri(e.Uri);

            e.Handled = true;
        }

        private TabInfo FindTabOfWebView2(WebView2 webView2)
        {
            foreach (TabInfo tab in Tabs)
            {
                if (tab.Content == webView2)
                    return tab;
            }
            return null;
        }

        private TabInfo GetCurrentTabInfo()
        {
            if (!Dispatcher.CheckAccess())
            {
                return Dispatcher.Invoke(() => BrowserTabs.SelectedItem as TabInfo);
            }
            return BrowserTabs.SelectedItem as TabInfo;
        }

        private WebView2 GetCurrentWebView()
        {
            if (!Dispatcher.CheckAccess())
            {
                return Dispatcher.Invoke(() => GetCurrentWebView());
            }
            var tabInfo = BrowserTabs.SelectedItem as TabInfo;
            return tabInfo?.Content as WebView2;
        }

        private void UpdateToolbarForCurrentTab()
        {
            var tabInfo = GetCurrentTabInfo();
            if (tabInfo != null && tabInfo.TabData != null)
            {
                var webView = tabInfo.TabData.WebView2;
                // Nếu user đang nhập trong ô URL (is editing) thì không ghi đè Url
                if (tabInfo.TabData.IsEditingUrl)
                {
                    // chỉ cập nhật trạng thái loading
                    tabInfo.TabData.IsLoading = webView?.CoreWebView2?.DocumentTitle == null;
                    return;
                }

                tabInfo.TabData.Url = webView?.Source?.ToString() ?? "";
                tabInfo.TabData.IsLoading = webView?.CoreWebView2?.DocumentTitle == null;
            }
        }

        private BitmapImage LoadIcon(string fileName)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Icon/{fileName}", UriKind.Absolute);
                return new BitmapImage(uri);
            }
            catch
            {
                return null;
            }
        }

        private void BrowserTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateToolbarForCurrentTab();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView != null && webView.CanGoBack)
                webView.GoBack();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView != null && webView.CanGoForward)
                webView.GoForward();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            var tabInfo = GetCurrentTabInfo();
            if (tabInfo != null && tabInfo.TabData != null)
            {
                if (tabInfo.TabData.IsLoading && tabInfo.TabData.WebView2?.CoreWebView2 != null)
                    tabInfo.TabData.WebView2.CoreWebView2.Stop();
                else
                    tabInfo.TabData.WebView2.Reload();
            }
        }

        private void HomeBtn_Click(object sender, RoutedEventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView != null)
                webView.Source = new Uri("https://www.google.com");
        }

        private void UrlAutoComplete_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as AutoCompleteBox;
            var selected = box.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selected))
            {
                // Chỉ nhập vào box, KHÔNG điều hướng!
                box.Text = selected;
                box.SelectedItem = null;
            }
        }

        private void UrlAutoComplete_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var box = sender as AutoCompleteBox;
                string input = box.Text.Trim();

                if (box.IsDropDownOpen && box.SelectedItem != null)
                {
                    var selectedSuggest = box.SelectedItem as string;
                    if (!string.IsNullOrWhiteSpace(selectedSuggest))
                    {
                        box.Text = selectedSuggest;
                        input = selectedSuggest;
                    }
                }

                bool isLikelyUrl = input.StartsWith("http://") || input.StartsWith("https://") ||
                    Regex.IsMatch(input, @"^[\w\-\.]+\.[a-z]{2,}(/.*)?$", RegexOptions.IgnoreCase);

                string url = input;
                if (isLikelyUrl && !input.StartsWith("http"))
                    url = "https://" + input;
                else if (!isLikelyUrl)
                    url = "https://www.google.com/search?q=" + Uri.EscapeDataString(input);

                var webView = GetCurrentWebView();
                if (webView != null)
                    webView.Source = new Uri(url);

                box.SelectedItem = null;
                e.Handled = true;
            }
        }

        private void UrlAutoComplete_TextChanged(object sender, RoutedEventArgs e)
        {
            var box = sender as AutoCompleteBox;
            string input = box.Text.Trim();

            Suggestions.Clear();

            // History
            var history = allHistoryList
                .Where(h => h.Title.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                            h.Url.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Select(h => h.Url)
                .Take(5);

            foreach (var h in history)
                Suggestions.Add(h);

            // Bookmark
            bookmarkManager.LoadBookmarks();
            var bookmarks = bookmarkManager.Bookmarks
                .Where(b => b.Title.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                            b.Url.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Url)
                .Take(3);

            foreach (var b in bookmarks)
                Suggestions.Add(b);

            // Static domains
            var staticDomains = new List<string> { "google.com", "github.com", "gmail.com", "golang.org" }
                .Where(s => s.StartsWith(input.ToLower()))
                .Select(s => $"https://{s}")
                .Take(2);

            foreach (var s in staticDomains)
                Suggestions.Add(s);

            // Search Google suggestion
            bool isLikelyUrl = input.StartsWith("http://") || input.StartsWith("https://") ||
                Regex.IsMatch(input, @"^[\w\-\.]+\.[a-z]{2,}(/.*)?$", RegexOptions.IgnoreCase);

            if (!isLikelyUrl && !string.IsNullOrWhiteSpace(input))
                Suggestions.Add($"Tìm kiếm với Google: {input}");
        }

        private void UrlAutoComplete_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is TabInfo tab)
            {
                tab.TabData.IsEditingUrl = true;
            }
        }

        private void UrlAutoComplete_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is TabInfo tab)
            {
                tab.TabData.IsEditingUrl = false;
            }
        }
        private void BookmarkBtn_Click(object sender, RoutedEventArgs e)
        {
            var tabInfo = GetCurrentTabInfo();
            if (tabInfo == null || tabInfo.TabData == null)
                return;

            if (tabInfo.IsIncognito)
            {
                System.Windows.MessageBox.Show("Tab ẩn danh không cho phép lưu bookmark.", "Incognito Mode", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var webView = tabInfo.TabData.WebView2;
            var title = webView.CoreWebView2?.DocumentTitle ?? "";
            var url = webView.Source?.ToString() ?? "";
            bookmarkManager.LoadBookmarks();

            var normalizedUrl = url.Trim().TrimEnd('/').ToLowerInvariant();
            var bookmark = bookmarkManager.Bookmarks.FirstOrDefault(
                b => b.Url.Trim().TrimEnd('/').ToLowerInvariant() == normalizedUrl);

            if (bookmark == null)
            {
                bookmark = new Bookmark
                {
                    Title = title,
                    Url = url,
                    IconUrl = GetFaviconUrl(url)
                };
            }

            var editor = new BookmarkEditorPopup(bookmark);
            editor.Owner = this;
            var result = editor.ShowDialog();
            if (result == true)
            {
                if (editor.IsDeleted)
                {
                    var bm = bookmarkManager.Bookmarks.FirstOrDefault(
                        b => b.Url.Trim().TrimEnd('/').ToLowerInvariant() == normalizedUrl);
                    if (bm != null)
                    {
                        bookmarkManager.RemoveBookmark(bm);
                    }
                }
                else
                {
                    if (!bookmarkManager.Bookmarks.Any(
                        b => b.Url.Trim().TrimEnd('/').ToLowerInvariant() == normalizedUrl))
                    {
                        bookmarkManager.AddBookmark(editor.Bookmark.Title, url);
                    }
                    else
                    {
                        var bm = bookmarkManager.Bookmarks.FirstOrDefault(
                            b => b.Url.Trim().TrimEnd('/').ToLowerInvariant() == normalizedUrl);
                        if (bm != null)
                        {
                            bm.Title = editor.Bookmark.Title;
                            bm.IconUrl = editor.Bookmark.IconUrl;
                            bookmarkManager.SaveBookmarks();
                        }
                    }
                }
            }
        }

        private void HistoryBtn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var tabInfo = GetCurrentTabInfo();
            if (tabInfo == null || tabInfo.TabData == null) return;

            var win = new HistoryPopup();
            win.CurrentTabHistory.Clear();
            foreach (var url in tabInfo.TabData.History.Reverse())
            {
                win.CurrentTabHistory.Add(new HistoryItem
                {
                    Title = GetTabHeaderFromUrl(url),
                    Description = url,
                    IconUrl = GetFaviconUrl(url),
                    Url = url
                });
            }
            win.GlobalHistory.Clear();
            foreach (var item in historyManager.GlobalHistory.AsEnumerable().Reverse())
            {
                win.GlobalHistory.Add(item);
            }
            win.Owner = this;
            var result = win.ShowDialog();
            if (result == true && !string.IsNullOrEmpty(win.SelectedUrl))
            {
                var webView = GetCurrentWebView();
                if (webView != null)
                    webView.Source = new Uri(win.SelectedUrl);
            }
        }

        private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is HistoryItem item)
            {
                var webView = GetCurrentWebView();
                if (webView != null)
                    webView.Source = new Uri(item.Url);

                HistoryPopupControl.IsOpen = false;
            }
        }

        private void NormalBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            Tabs.Add(CreateNewTab());
            InfoPanel.Visibility = Visibility.Collapsed;
        }

        private void IncognitoBtn_Click(object sender, RoutedEventArgs e)
        {
            var tab = CreateNewTab();
            tab.IsIncognito = true;
            tab.Header = "Ẩn danh";
            Tabs.Add(tab);
            BrowserTabs.SelectedItem = tab;
            InfoPanel.Visibility = Visibility.Collapsed;
        }

        private string GetFaviconUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.Host}/favicon.ico";
            }
            catch
            {
                return "/Icon/web.png";
            }
        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView != null && webView.CoreWebView2 != null)
            {
                string html = await webView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
                html = JsonSerializer.Deserialize<string>(html);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "HTML files (*.html)|*.html",
                    FileName = "TrangWeb.html"
                };
                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, html);
                    System.Windows.MessageBox.Show("Đã lưu trang web!", "Tải về trang web", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("No active web view found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CoreWebView2_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            WebView2 webView2Sender = null;
            foreach (var tab in Tabs)
            {
                if (tab.Content is WebView2 wv && wv.CoreWebView2 == sender as CoreWebView2)
                {
                    webView2Sender = wv;
                    break;
                }
            }
            var tabInfo = webView2Sender != null ? FindTabOfWebView2(webView2Sender) : null;

            if (tabInfo != null && tabInfo.IsIncognito)
            {
                return;
            }

            var item = new DownloadedItem
            {
                FileName = System.IO.Path.GetFileName(e.ResultFilePath),
                FilePath = e.ResultFilePath,
                FileSize = 0,
                DownloadedAt = DateTime.Now,
            };

            HistoryDownloaded.Add(item);
            DownloadHistoryManager.SaveDownloadHistory(HistoryDownloaded, HistoryFilePath);

            e.DownloadOperation.BytesReceivedChanged += (s, args) =>
            {
                item.FileSize = e.DownloadOperation.BytesReceived;
                DownloadHistoryManager.SaveDownloadHistory(HistoryDownloaded, HistoryFilePath);
            };
        }

        private void ShowDownloadHistory_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var popup = new DownloadedPopup(HistoryDownloaded);
            popup.Owner = this;
            popup.ShowDialog();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow();
            win.Owner = this;
            var result = win.ShowDialog();
        }
        // trong class WebWindow (MyWebBrowser.WebWindow)
        public void ApplySettings(MyWebBrowser.Setting.AppSettings settings)
        {
            if (settings == null) return;
            try
            {
                if (string.Equals(settings.Theme, "Dark", StringComparison.OrdinalIgnoreCase))
                    this.Background = System.Windows.Media.Brushes.Black;
                else
                    this.Background = System.Windows.Media.Brushes.WhiteSmoke;
            }
            catch { }

            // Zoom: apply cho các webview đã khởi tạo
            foreach (var tab in Tabs)
            {
                var wv = tab.TabData?.WebView2;
                if (wv?.CoreWebView2 != null)
                {
                    try
                    {
                        wv.ZoomFactor = settings.DefaultZoomPercent / 100.0;
                    }
                    catch { }
                }
            }

        }

        private void MoreBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn?.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BrowserTabs_ClosingItem(ItemActionCallbackArgs<TabablzControl> args)
        {
            var tab = args.DragablzItem?.Content as TabInfo;
            if (tab != null && tab.IsIncognito && !string.IsNullOrEmpty(tab.IncognitoProfilePath))
            {
                try
                {
                    if (System.IO.Directory.Exists(tab.IncognitoProfilePath))
                        System.IO.Directory.Delete(tab.IncognitoProfilePath, true);
                }
                catch { }
            }

            if (Tabs.Count == 1)
            {
                this.Close();
                return;
            }
        }

        private void SavedBookmark_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            bookmarkManager.LoadBookmarks();

            var win = new BookmarkPopup();
            win.Bookmarks.Clear();
            foreach (var bm in bookmarkManager.Bookmarks)
            {
                win.Bookmarks.Add(new Bookmark
                {
                    Title = bm.Title,
                    Url = bm.Url,
                    IconUrl = GetFaviconUrl(bm.Url)
                });
            }

            win.Owner = this;
            var result = win.ShowDialog();
            if (result == true && !string.IsNullOrEmpty(win.SelectedUrl))
            {
                var webView = GetCurrentWebView();
                if (webView != null)
                    webView.Source = new Uri(win.SelectedUrl);
            }
        }

        private async void SummarizeAI_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView == null || webView.Source == null)
            {
                System.Windows.MessageBox.Show("Không có trang web nào được mở.");
                return;
            }

            if (isSummarizingAI)
                return;

            isSummarizingAI = true;
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                string url = webView.Source.ToString();
                string summary = await SummarizeWithAIAsync(url);

                var summaryWindow = new SummaryAI(summary);
                summaryWindow.Owner = this;
                summaryWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Có lỗi khi tóm tắt AI: " + ex.Message);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                isSummarizingAI = false;
            }
        }

        // Hàm gọi API Gemini
        private async Task<string> SummarizeWithAIAsync(string url)
        {
            // TODO: Không để API key cứng trong mã nguồn. Đưa vào cấu hình/secret store.
            string apiKey = "AIzaSyAD9S6dWcbyXDhI2oAl-QUYxrKl2d-ArpE";
            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"Hãy tóm tắt nội dung chính của trang web sau: {url}" }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var client = new HttpClient();

            var response = await client.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
            var result = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(result);
                var summary = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                return summary ?? "No result!";
            }
            catch
            {
                return $"Error:\n{result}";
            }
        }

    }
}