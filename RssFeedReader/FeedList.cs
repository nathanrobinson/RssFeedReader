using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RssFeedReader
{
    class FeedList
    {
        public FeedList()
        {
            Feeds = new List<FeedDefinition>();
        }
        public FeedList(XContainer document)
        {
            XContainer container = document.Element("feedList") ?? document;

            var feeds = container.Elements("feed");

            if (feeds != null && feeds.Any())
                Feeds = feeds.Select(e => new FeedDefinition(e)).ToList();
            else
                Feeds = new List<FeedDefinition>();

            var start = container.Element("disabledStartTime");
            if(start != null)
            {
                DateTime startTime;
                if(DateTime.TryParse(DateTime.Today.ToShortDateString() + " " + start.Value, out startTime))
                {
                    DisabledStartTime = startTime;

                    var end = container.Element("disabledEndTime");
                    if(end != null)
                    {
                        DateTime endTime;
                        if(DateTime.TryParse(DateTime.Today.ToShortDateString() + " " + end.Value, out endTime))
                        {
                            DisabledEndTime = endTime;
                        }
                    }
                }
            }
        }

        public void Merge(XContainer document)
        {
            XContainer container = document.Element("feedList") ?? document;

            FeedDefinition[] feeds = container.Elements("feed").Select(e => new FeedDefinition(e)).ToArray();
            foreach(FeedDefinition feed in feeds)
            {
                FeedDefinition feed1 = feed;
                var mainFeed = Feeds.Where(f => f.Url == feed1.Url).FirstOrDefault();
                if (mainFeed != null)
                    feed.LastUpdate = mainFeed.LastUpdate;
            }
            Feeds = feeds.ToList();

            var start = container.Element("disabledStartTime");
            if (start != null)
            {
                DateTime startTime;
                if (DateTime.TryParse(DateTime.Today.ToShortDateString() + " " + start.Value, out startTime))
                {
                    DisabledStartTime = startTime;

                    var end = container.Element("disabledEndTime");
                    if (end != null)
                    {
                        DateTime endTime;
                        if (DateTime.TryParse(DateTime.Today.ToShortDateString() + " " + end.Value, out endTime))
                        {
                            DisabledEndTime = endTime;
                        }
                    }
                }
            }
        }

        public List<FeedDefinition> Feeds { get; set; }
        public DateTime? DisabledStartTime { get; set; }
        public DateTime? DisabledEndTime { get; set; }
        public bool IsDisabled
        {
            get
            {
                DateTime now = DateTime.Now;
                return DisabledStartTime.HasValue
                       && DisabledEndTime.HasValue
                       && ( ( DisabledStartTime.Value.TimeOfDay > DisabledEndTime.Value.TimeOfDay
                              && ( now.TimeOfDay >= DisabledStartTime.Value.TimeOfDay
                                   || now.TimeOfDay <= DisabledEndTime.Value.TimeOfDay ) )
                            || ( now.TimeOfDay >= DisabledStartTime.Value.TimeOfDay
                                 && now.TimeOfDay <= DisabledEndTime.Value.TimeOfDay ) );
            }
        }
    }
}
