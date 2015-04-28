using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using RssFeedReader.Properties;

namespace RssFeedReader
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class FeedWindow
    {
        public FeedWindow()
        {
            InitializeComponent();
        }

        private delegate void UpdateTimerDelegate(object state);
        private void UpdateTimer(object state)
        {
            if(!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, (UpdateTimerDelegate)UpdateTimer, state);
            }
            else
            {
                tbTimeLeft.Text = _bfr.TimeLeft.Minutes + ":"+_bfr.TimeLeft.Seconds.ToString("00");
            }
        }

        private Timer _timer;
        private BackgroundFeedReader _bfr;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _bfr = new BackgroundFeedReader
            {
                FeedListFile = Settings.Default.FeedListFile,
                RefreshMinutes = Settings.Default.RefreshMinutes
            };
            tvChannels.ItemsSource = _bfr.Feeds;
            _bfr.Feeds.ListChanged += Feeds_ListChanged;
            _timer = new Timer(UpdateTimer, null, 0, 1000);

            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.BalloonTipText = "The app has been minimised. Click the tray icon to show.";
            _notifyIcon.BalloonTipTitle = "RSS Feed Reader";
            _notifyIcon.Text = "RSS Feed Reader";
            _notifyIcon.Icon = Properties.Resources.rss;
            _notifyIcon.Click += new EventHandler(_notifyIcon_Click);
            _notifyIcon.Visible = true;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        void _notifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        void Feeds_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            if(IsActive || !_bfr.Feeds.Any(f8 => f8.NewChildren > 0))
                return;

            Storyboard windowAlert = (Storyboard)FindResource("TitleAlert");
            windowAlert.Begin(this);
        }

        private void Expander_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Process.OpenUrl(((RssFeedItem)((((Expander)sender)).Header)).Link);
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.OpenUrl(((TextBox)sender).Text);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            ((RssFeedItem)((((Expander)sender)).Header)).IsNew = false;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MainWindow_Activated(object sender, System.EventArgs e)
        {
            if (IsActive)
            {
                Storyboard sdb = (Storyboard)FindResource("FadeIn");
                sdb.Begin(this);
                Storyboard windowAlert = (Storyboard)FindResource("StopTitleAlert");
                windowAlert.Begin(this);
            }
            else
            {
                Storyboard sdb = (Storyboard)FindResource("FadeOut");
                sdb.Begin(this);
            }
        }

        private void TextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var items = ( (RssFeed)( (ContentPresenter)( ( (FrameworkElement)( ( (FrameworkElement)( sender ) ).Parent ) ).TemplatedParent ) ).Content ).Items;
            foreach(var item in items)
                item.IsNew = false;
        }
    }
}
