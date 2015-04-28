using System.Windows;

namespace RssFeedReader
{
    /// <summary>
    /// Interaction logic for Link.xaml
    /// </summary>
    public partial class Link 
    {
        public Link()
        {
            InitializeComponent();
        }

        public string Url { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Url))
                Process.OpenUrl(Url);
        }
    }
}
