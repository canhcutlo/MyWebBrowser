using System.Collections.Generic;
using System.Windows;

namespace MyWebBrowser
{
    public partial class SummaryAI : Window
    {
        public SummaryAI(string summary)
        {
            InitializeComponent();
            DataContext = new
            {
                Summaries = new List<SummaryItem> {
                new SummaryItem { Title = "Tóm tắt", Content = summary }
            }
            };
        }


        public SummaryAI(IEnumerable<SummaryItem> summaries)
        {
            InitializeComponent();
            DataContext = new { Summaries = summaries };
        }

        private bool _isClosing = false;
        public bool IsClosing => _isClosing;
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;
            base.OnClosing(e);
        }
        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!this.IsClosing)
            {
                this.Close();
            }
        }
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class SummaryItem
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }
}