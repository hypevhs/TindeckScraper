using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;

namespace TindeckScraper
{
    internal static class Program
    {
        private static IBrowsingContext _browsingContext;
        private static WebClient _webClient;

        [STAThread]
        private static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            //setup
            _browsingContext = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
            _webClient = new WebClient();
            var doc = await _browsingContext.OpenAsync("http://tindeck.com/users/geckoyamori");

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
                var trackId = HrefToId(trackHref);
                await DownloadTrackById(trackId);
            }

            Console.ReadLine();
        }

        private static async Task<bool> DownloadTrackById(string trackId)
        {
            var doc = await _browsingContext.OpenAsync($"http://tindeck.com/dl/{trackId}");
            var queryResults = doc.QuerySelectorAll("div.content span a[href*=download]");
            var directLinkEl = queryResults.FirstOrDefault();
            if (directLinkEl == null)
                return false;
            var downloadUrl = directLinkEl.GetAttribute("href");
            DownloadUrl(doc.Origin + downloadUrl, trackId);
            return true;
        }

        private static void DownloadUrl(string trackUrl, string trackId)
        {
            var fileName = trackId + GetBaseNameFromUrl(trackUrl);
            Console.WriteLine($"Downloading to {fileName}...");
            _webClient.DownloadFile(new Uri(trackUrl), fileName);
        }

        private static string GetBaseNameFromUrl(string trackUrl)
        {
            return trackUrl.Substring(trackUrl.LastIndexOf("/", StringComparison.Ordinal) + 1);
        }

        private static string HrefToId(string trackHref)
        {
            //extract with regex
            var re = new Regex("/listen/(.*)");
            var matches = re.Match(trackHref);
            if (!matches.Success)
                throw new FormatException();

            //get the capture group
            return matches.Groups[1].Value;
        }

        /// <summary>
        /// Check how many we found against the table header that says "Uploads (123)"
        /// </summary>
        private static bool FoundExpectedNumber(IDocument doc, int howManyFound)
        {
            var tableHeaderCount = GetTableHeader(doc);

            //compare expected,actual
            return tableHeaderCount == howManyFound;
        }

        /// <summary>
        /// Get the table header that says "Uploads (123)"
        /// </summary>
        private static int GetTableHeader(IDocument doc)
        {
            //get the element
            var countElement = doc.QuerySelectorAll("#upla").FirstOrDefault();
            if (countElement == null)
                throw new FormatException();

            //extract with regex
            var re = new Regex(@".*\((\d+)\).*");
            var matches = re.Match(countElement.InnerHtml);
            if (!matches.Success)
                throw new FormatException();

            //get the capture group
            var firstGroup = matches.Groups[1];
            var countText = firstGroup.Value;

            //parse into int
            return int.Parse(countText);
        }
    }
}
