using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace RssFeedReader
{
    class RssFeedItem : INotifyPropertyChanged
    {
        //private static readonly XNamespace slash = "http://purl.org/rss/1.0/modules/slash/";
        private static readonly XNamespace content = "http://purl.org/rss/1.0/modules/content/";
        public RssFeedItem()
        {
            IsNew = true;
        }

        public RssFeedItem(XContainer i) : this()
        {
            var pubDate = i.Element("pubDate");
                DateTime pd;
            if (pubDate != null
                && DateTime.TryParse(pubDate.Value, out pd))
                Posted = pd;

            var title = i.Element("title");
            if (title != null)
                Title = title.Value;

            var description = i.Element("content") ?? i.Element(content + "encoded") ?? i.Element("description");
            if (description != null)
                Description = description.Value;

            var link = i.Element("link");
            if(link != null)
                Link = link.Value;
        }

        private DateTime _posted;
        public DateTime Posted
        {
            get { return _posted; }
            set
            {
                if (_posted == value)
                    return;

                _posted = value;
                NotifyPropertyChanged("Posted");
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                string text = HttpUtility.HtmlDecode(value);
                if (_title == text)
                    return;

                _title = text;
                NotifyPropertyChanged("Title");
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description == value)
                    return;

                _description = value;
                NotifyPropertyChanged("Description");
                NotifyPropertyChanged("DescriptionControl");
                NotifyPropertyChanged("DescriptionText");
                _descriptionText = null;
                _descriptionControl = null;
            }
        }

        private const RegexOptions REGEXOPTIONS = RegexOptions.Singleline & RegexOptions.IgnoreCase & RegexOptions.Compiled;
        private readonly Regex _makeParagraphs = new Regex("<p.*?>", REGEXOPTIONS);
        private readonly Regex _makeImages = new Regex("(?ms)(.*?)<img.*?src=\"(.*?)\".*?\\s*((title|alt)=\"[^\"]*\"\\s*)*.*?(/>|></img>)", REGEXOPTIONS);
        private readonly Regex _makeLinks = new Regex("(?ms)(.*?)<a.*?href=\"(http.?://.*?)\".*?>(.*?)</a>", REGEXOPTIONS);
        private readonly Regex _makeBlockQuotes = new Regex("(?ms)(.*?)<blockquote.*?>(.*?)</blockquote>", REGEXOPTIONS);
        private readonly Regex _stripHtml = new Regex("<.*?>", REGEXOPTIONS);
        private readonly Regex _hiddenLink = new Regex(@"(http://\S*)", REGEXOPTIONS);

        private string _descriptionText;
        public string DescriptionText
        {
            get
            {
                if (string.IsNullOrEmpty(_descriptionText))
                {
                    _descriptionText = _makeBlockQuotes.Replace(Description, Environment.NewLine + "  \"$1\"" + Environment.NewLine);
                    _descriptionText = _makeParagraphs.Replace(_descriptionText, Environment.NewLine);
                    _descriptionText = _stripHtml.Replace(_descriptionText, " ");
                    _descriptionText = HttpUtility.HtmlDecode(_descriptionText);
                }
                return _descriptionText;
            }
        }

        private UIElement _descriptionControl;
        public UIElement DescriptionControl
        {
            get
            {
                if (_descriptionControl == null)
                {
                    string text = HttpUtility.HtmlDecode(Description);
                    text = _makeParagraphs.Replace(text, Environment.NewLine);
                    _descriptionControl = ParseHtmlFragment(text);
                }
                return _descriptionControl;
            }
        }

        private UIElement MakeLink(string fragment)
        {
            List<UIElement> controls = new List<UIElement>();

            var matches = _makeLinks.Matches(fragment);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    UIElement previous = ParseHtmlFragment(match.Groups[1].Captures[0].Value);
                    if (previous != null)
                        controls.Add(previous);
                    UIElement linkContent = ParseHtmlFragment(match.Groups[3].Captures[0].Value)
                                        ?? new TextBlock
                                        {
                                            Text = match.Groups[2].Captures[0].Value,
                                            TextWrapping = TextWrapping.WrapWithOverflow
                                        };

                    controls.Add(new Link
                    {
                        Url = match.Groups[2].Captures[0].Value,
                        Content = linkContent
                    });
                }
                var lastmatch = matches[matches.Count - 1].Groups[0];
                string text = fragment.Substring(lastmatch.Index + lastmatch.Length);
                if (!string.IsNullOrEmpty(text))
                    controls.Add(ParseHtmlFragment(text));

                if (controls.Count == 1)
                    return controls[0];
                if (controls.Count < 1)
                    return null;

                StackPanel sp = new StackPanel();
                foreach (FrameworkElement control in controls)
                {
                    sp.Children.Add(control);
                }
                return sp;
            }
            return null;
        }

        private UIElement MakeImage(string fragment)
        {
            List<UIElement> controls = new List<UIElement>();

            var matches = _makeImages.Matches(fragment);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    UIElement previous = ParseHtmlFragment(match.Groups[1].Captures[0].Value);
                    if (previous != null)
                        controls.Add(previous);
                    BitmapImage bmp = new BitmapImage {
                                                          DecodePixelWidth = 300
                                                      };
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(match.Groups[2].Captures[0].Value);
                    bmp.EndInit();
                    controls.Add(new Image
                    {
                        Source = bmp,
                        MaxWidth = 300
                    });
                    if(match.Groups.Count >= 4)
                    {
                        foreach (var capture in match.Groups[3].Captures)
                            controls.Add(
                                new TextBlock {
                                                  Text = capture.ToString(),
                                                  TextWrapping = TextWrapping.WrapWithOverflow
                                              });
                    }
                }
                var lastmatch = matches[matches.Count - 1].Groups[0];
                string text = fragment.Substring(lastmatch.Index + lastmatch.Length);
                if (!string.IsNullOrEmpty(text))
                    controls.Add(ParseHtmlFragment(text));

                if (controls.Count == 1)
                    return controls[0];
                if (controls.Count < 1)
                    return null;

                StackPanel sp = new StackPanel();
                foreach (FrameworkElement control in controls)
                {
                    sp.Children.Add(control);
                }
                return sp;
            }
            return null;
        }

        private UIElement MakeQuote(string fragment)
        {
            List<UIElement> controls = new List<UIElement>();

            var matches = _makeBlockQuotes.Matches(fragment);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    UIElement previous = ParseHtmlFragment(match.Groups[1].Captures[0].Value);
                    if (previous != null)
                        controls.Add(previous);
                    controls.Add(new TextBlock
                    {
                        Text = _stripHtml.Replace(match.Groups[2].Captures[0].Value, string.Empty),
                        Margin = new Thickness(15, 5, 5, 5),
                        Background = Brushes.Azure,
                        TextWrapping = TextWrapping.WrapWithOverflow
                    });
                }
                var lastmatch = matches[matches.Count - 1].Groups[0];
                string text = fragment.Substring(lastmatch.Index + lastmatch.Length);
                if (!string.IsNullOrEmpty(text))
                    controls.Add(ParseHtmlFragment(text));

                if (controls.Count == 1)
                    return controls[0];
                if (controls.Count < 1)
                    return null;

                StackPanel sp = new StackPanel();
                foreach (FrameworkElement control in controls)
                {
                    sp.Children.Add(control);
                }
                return sp;
            }
            return null;
        }

        private UIElement ParseHtmlFragment(string fragment)
        {
            if (string.IsNullOrEmpty(fragment))
                return null;

            if (!_stripHtml.IsMatch(fragment))
            {
                if(!_hiddenLink.IsMatch(fragment))

                    return new TextBlock {
                                             Text = fragment,
                                             TextWrapping = TextWrapping.WrapWithOverflow
                                         };

                return MakeLink(_hiddenLink.Replace(fragment, "<a href=\"$1\"></a>"));
            }

            UIElement icontrol = MakeLink(fragment)
                ?? MakeImage(fragment)
                ?? MakeQuote(fragment);

            if (icontrol != null)
                return icontrol;

            fragment = _stripHtml.Replace(fragment, string.Empty);
            if (string.IsNullOrEmpty(fragment))
                return new Separator();

            return new TextBlock
            {
                Text = fragment,
                TextWrapping = TextWrapping.WrapWithOverflow
            };
        }

        private string _link;
        public string Link
        {
            get { return _link; }
            set
            {
                if (_link == value)
                    return;

                _link = value;
                NotifyPropertyChanged("Link");
            }
        }

        private bool _isNew;
        public bool IsNew
        {
            get { return _isNew; }
            set
            {
                if (_isNew == value)
                    return;

                _isNew = value;
                NotifyPropertyChanged("IsNew");
            }
        }

        #region Implementation of INotifyPropertyChanged

        private void NotifyPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
