using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;

namespace TindeckScraper
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            //setup
            var conf = Configuration.Default.WithDefaultLoader();
            var url = "http://tindeck.com/users/geckoyamori";
            var doc = await BrowsingContext.New(conf).OpenAsync(url);

            //get track links
            var tracklinks = doc.QuerySelectorAll("#usrlist" +
                                             " td[align='left']" +
                                             " a[href*='listen']"); //href contains listen
            
            //check that it's the same number at the top of the page
            var goodNum = FoundExpectedNumber(doc, tracklinks.Length);
            if (!goodNum)
            {
                Console.WriteLine("Didn't find the same number of tracks we were expecting");
                return;
            }

            foreach (var track in tracklinks)
            {
                var trackName = track.InnerHtml;

                Console.WriteLine($"Track: {trackName,50}");

                var trackHref = track.GetAttribute("href");
                string trackId;
                if (TryHrefToId(trackHref, out trackId))
                {
                    Console.WriteLine("Track is broken.");
                    continue;
                }

                var result = DownloadTrackById(trackId);
            }

            Console.ReadLine();
        }

        private static bool DownloadTrackById(string trackId)
        {
            var url = $"http://tindeck.com/dl/{trackId}";
            //TODO
            return false;
        }

        private static bool TryHrefToId(string trackHref, out string trackId)
        {
            trackId = string.Empty;

            //extract with regex
            var re = new Regex("/listen/(.*)");
            var matches = re.Match(trackHref);
            if (!matches.Success)
                return false;

            //get the capture group
            trackId = matches.Groups[1].Value;
            return true;
        }

        /// <summary>
        /// Check how many we found against the table header that says "Uploads (123)"
        /// </summary>
        private static bool FoundExpectedNumber(IDocument doc, int howManyFound)
        {
            int resultCount;
            if (!TryGetTableHeader(doc, out resultCount)) return false;

            //compare expected,actual
            return resultCount == howManyFound;
        }

        /// <summary>
        /// Get the table header that says "Uploads (123)"
        /// </summary>
        private static bool TryGetTableHeader(IDocument doc, out int resultCount)
        {
            resultCount = -1;

            //get the element
            var countElement = doc.QuerySelectorAll("#upla").FirstOrDefault();
            if (countElement == null)
                return false;

            //extract with regex
            var re = new Regex(@".*\((\d+)\).*");
            var matches = re.Match(countElement.InnerHtml);
            if (!matches.Success)
                return false;

            //get the capture group
            var firstGroup = matches.Groups[1];
            var countText = firstGroup.Value;

            //parse into int
            return int.TryParse(countText, out resultCount);
        }
    }
}
