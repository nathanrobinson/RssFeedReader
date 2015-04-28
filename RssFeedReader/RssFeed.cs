using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Linq;

namespace RssFeedReader
{
    class RssFeed : INotifyPropertyChanged
    {
        private readonly Timer timer;
        public RssFeed()
        {
            timer = new Timer(o => NotifyPropertyChanged("NextUpdateCountdown"));
            timer.Change(60000, 60000);
        }
        ~RssFeed()
        {
            timer.Dispose();
        }
        
        public RssFeed(XContainer document) : this()
        {
            var channel = document.Descendants("channel").First() ?? document.Element("channel");
            if(channel == null)
                return;

            var title = channel.Element("title");
            if (title != null)
                Title = title.Value;

            var items = channel.Elements("item").Take(5).Select(i => new RssFeedItem(i));

            Items = new BindingList<RssFeedItem>(items.ToList());
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if(_title == value)
                    return;
                _title = value;
                NotifyPropertyChanged("Title");
            }
        }

        private BindingList<RssFeedItem> _items;
        public BindingList<RssFeedItem> Items
        {
            get { return _items; }
            set
            {
                if (_items == value)
                    return;
                _items = value;
                NotifyPropertyChanged("Items");
                _items.ListChanged += _items_ListChanged;
                NotifyPropertyChanged("NewChildren");
            }
        }

        public int NewChildren
        {
            get
            {
                return Items.Count(i => i.IsNew);
            }
        } 

        void _items_ListChanged(object sender, ListChangedEventArgs e)
        {
            NotifyPropertyChanged("Items");
            NotifyPropertyChanged("NewChildren");
        }

        private int _updateInterval;
        public int UpdateInterval
        {
            get { return _updateInterval; }
            set
            {
                if(_updateInterval == value)
                    return;
                _updateInterval = value;
                NotifyPropertyChanged("UpdateInterval");
            }
        }

        private DateTime _lastUpdate;
        public DateTime LastUpdate
        {
            get { return _lastUpdate; }
            set
            {
                if(_lastUpdate == value)
                    return;
                _lastUpdate = value;
                NotifyPropertyChanged("LastUpdate");
            }
        }

        public string NextUpdateCountdown
        {
            get
            {
                TimeSpan ts = (LastUpdate.AddMinutes(UpdateInterval) - DateTime.Now);
                return ((int)ts.TotalMinutes).ToString();
            }
        }

        #region Implementation of INotifyPropertyChanged

        public Dispatcher Dispatcher { get; set; }
        private delegate void DelegateNotifyPropertyChanged(string propertyName);
        private void NotifyPropertyChanged(string propertyName)
        {
            if (Dispatcher != null && !Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke((DelegateNotifyPropertyChanged)NotifyPropertyChanged, DispatcherPriority.DataBind, propertyName);
            }
            else
            {
                if(PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
