using System;
using System.Xml.Linq;

namespace RssFeedReader
{
    class FeedDefinition
    {
        public FeedDefinition()
        {
            UpdateInterval = 15;
        }
        public FeedDefinition(XElement element)
        {
            var link = element.Attribute("href");
            if (link != null)
                Url = link.Value;

            var interval = element.Attribute("interval");
            if(interval != null)
            {
                int updateInterval;
                if (int.TryParse(interval.Value, out updateInterval))
                    UpdateInterval = updateInterval;
            }
            if (UpdateInterval <= 0)
                UpdateInterval = 15;
        }
        public string Url { get; set; }
        public int UpdateInterval { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
