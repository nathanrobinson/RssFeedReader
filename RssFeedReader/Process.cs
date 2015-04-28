namespace RssFeedReader
{
    public class Process
    {
        public static void OpenUrl(string url)
        {
            try
            {
                if (url.ToLower().StartsWith("http://"))
                    System.Diagnostics.Process.Start(url);
            }
            catch { }
        }
    }
}