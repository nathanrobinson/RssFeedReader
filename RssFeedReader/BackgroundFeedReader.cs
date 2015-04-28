using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.Win32;

namespace RssFeedReader
{
    class BackgroundFeedReader : IDisposable
    {
        public BackgroundFeedReader()
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (_timer == null)
                return;
            if (e.Reason == SessionSwitchReason.SessionLock)
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
                _timer.Change(new TimeSpan(0), _timerSpan);
        }

        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        public string FeedListFile { get; set; }
        public string[] FeedUrls { get; set; }

        private TimeSpan _timerSpan = new TimeSpan(0, 0, 1, 0);
        public double RefreshMinutes
        {
            get
            {
                return _timerSpan.TotalMinutes;
            }
            set
            {
                if (_timerSpan.TotalMinutes == value)
                    return;

                _timerSpan = TimeSpan.FromMinutes(value);

                if (_timer == null)
                    _timer = new Timer(c => ThreadFillList(), null, new TimeSpan(0), _timerSpan);
                else
                    _timer.Change(new TimeSpan(0), _timerSpan);
            }
        }

        private DateTime _lastRun;
        public TimeSpan TimeLeft
        {
            get
            {
                return _timerSpan - (DateTime.Now - _lastRun);
            }
        }
        
        private Timer _timer;
        private Thread _thread;

        private BindingList<RssFeed> _rssFeed;
        public BindingList<RssFeed> Feeds
        {
            get
            {
                if (_rssFeed == null)
                {
                    _rssFeed = new BindingList<RssFeed>();
                    if (_timer == null)
                        _timer = new Timer(c => ThreadFillList(), null, new TimeSpan(0), _timerSpan);
                }
                return _rssFeed;
            }
        }

        private void ThreadFillList()
        {
            _thread = new Thread(FillFeeds);
            _thread.Start();

            _lastRun = DateTime.Now;
        }

        private FeedList _feeds;
        private void FillFeeds()
        {
            if (FeedListFile.ToLower().EndsWith(".xml"))
                FillXmlFeeds();
            else
                FillTextFeeds();

            if (_feeds.IsDisabled)
                return;

            List<RssFeed> feeds = new List<RssFeed>();
            foreach (FeedDefinition feed in _feeds.Feeds)
            {
                if (feed.LastUpdate.AddMinutes(feed.UpdateInterval) <= DateTime.Now)
                    try
                    {
                        XDocument rssFeed = XDocument.Load(feed.Url);
                        feeds.Add(new RssFeed(rssFeed) {
                                                           LastUpdate = ( feed.LastUpdate = DateTime.Now ),
                                                           UpdateInterval = feed.UpdateInterval,
                                                           Dispatcher = _dispatcher
                                                       });
                    }
                    catch { }
            }
            SafeMergeFeeds(feeds);
        }

        private void FillTextFeeds()
        {
            List<string> urls = new List<string>();
            lock (FeedListFile)
            {
                if (!string.IsNullOrEmpty(FeedListFile))
                {
                    using (StreamReader sr = new StreamReader(File.Open(FeedListFile, FileMode.Open)))
                        while (!sr.EndOfStream)
                            urls.Add(sr.ReadLine());
                }
            }
            if (urls.Count <= 0)
            {
                lock (FeedUrls)
                {
                    if (FeedUrls == null || FeedUrls.Length <= 0)
                        throw new Exception("No Feeds Found.");

                    urls.AddRange(FeedUrls);
                }
            }
            if (_feeds == null)
                _feeds = new FeedList();
            _feeds.Feeds = urls.Select(u => new FeedDefinition
            {
                Url = u
            }).ToList();
        }
        
        private void FillXmlFeeds()
        {
            lock (FeedListFile)
            {

                if (_feeds == null)
                    _feeds = new FeedList(XDocument.Load(FeedListFile));
                else
                    _feeds.Merge(XDocument.Load(FeedListFile));
            }
        }

        private delegate void DelegateSafeMergeFeeds(IEnumerable<RssFeed> feeds);
        private void SafeMergeFeeds(IEnumerable<RssFeed> feeds)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.Invoke((DelegateSafeMergeFeeds)SafeMergeFeeds, DispatcherPriority.DataBind, feeds);
            }
            else
            {
                foreach (RssFeed feed in feeds)
                {
                    RssFeed feed1 = feed;
                    var matches = _rssFeed.Where(f => f.Title == feed1.Title);
                    if (!matches.Any())
                        _rssFeed.Add(feed1);
                    else
                    {
                        var match = matches.First();
                        match.LastUpdate = feed.LastUpdate;
                        match.UpdateInterval = feed.UpdateInterval;
                        foreach (RssFeedItem item in feed.Items.OrderBy(i => i.Posted))
                        {
                            RssFeedItem item1 = item;
                            var itemMatches = match.Items.Where(i => i.Title == item1.Title);
                            if (!itemMatches.Any())
                            {
                                match.Items.Insert(0, item1);
                                //var oldItems = match.Items.Where(i => i.Posted < DateTime.Now.AddDays(-2));
                                //while (match.Items.Count > 5 && oldItems.Any())
                                //    match.Items.Remove(oldItems.Last());
                            }
                            else
                            {
                                var itemMatch = itemMatches.First();
                                itemMatch.Link = item1.Link;
                                itemMatch.Posted = item.Posted;
                                itemMatch.Description = item.Description;
                            }
                        }
                        while (match.Items.Count > 5)
                            match.Items.Remove(match.Items.Last());
                    }
                }
            }
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
        }

        #endregion
    }
}
