using HtmlAgilityPack;
using NUnit.Framework;
using DataAutoFramework.Helper;
using NUnit.Framework.Legacy;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Playwright;

namespace DataAutoFramework.TestCases
{
    public class TestPageLinks
    {
        public static List<string> TestLinks { get; set; }

        public static Dictionary<string, string> SpecialLinks { get; set; }

        static TestPageLinks()
        {
            TestLinks = JsonSerializer.Deserialize<List<string>>(File.ReadAllText("appsettings.json")) ?? new List<string>();

            SpecialLinks = new Dictionary<string, string>();

            SpecialLinks.Add("Read in English", "https://learn.microsoft.com/en-us/python/api/overview/azure/app-configuration?view=azure-python");
            SpecialLinks.Add("our contributor guide", "https://github.com/Azure/azure-sdk-for-python/blob/main/CONTRIBUTING.md");
            // SpecialLinks.Add("English (United States)", "/en-us/locale?target=https%3A%2F%2Flearn.microsoft.com%2Fen-us%2Fpython%2Fapi%2Foverview%2Fazure%2Fapp-configuration%3Fview%3Dazure-python");
            SpecialLinks.Add("Privacy", "https://go.microsoft.com/fwlink/?LinkId=521839");
        }
        
        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public async Task TestBrokenLinks(string testLink)
        {

            var baseUri =  testLink.Substring(0, testLink.LastIndexOf("/"));
            var errorList = new List<string>();
            var web = new HtmlWeb();
            var doc = web.Load(testLink);
            foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                var linkValue = link.Attributes["href"].Value;
                if (linkValue.StartsWith("#"))
                {
                    linkValue = testLink + linkValue;
                }
                else if (!linkValue.StartsWith("#") && !linkValue.StartsWith("http") && !linkValue.StartsWith("https"))
                {
                    linkValue = baseUri + "/" + linkValue;
                }
                if(!await ValidationHelper.CheckIfPageExist(linkValue))
                {
                    errorList.Add(link.OuterHtml);
                }
            }
            
            ClassicAssert.Zero(errorList.Count, testLink + " has error link at " + string.Join(",", errorList));
        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public async Task TestCrossLinks(string testLink)
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(testLink);

            var hrefs = page.Locator("#main-column").Locator("a");
            var failCount = 0;
            var failMsg = "";

            for(var index = 0; index < await hrefs.CountAsync(); index++)
            {
                var href = hrefs.Nth(index);
                var attri = href.GetAttributeAsync("href").Result;
                var text = href.InnerTextAsync().Result;

                if (String.IsNullOrEmpty(text.Trim()) || text.Trim() == "English (United States)")
                {
                    continue;
                }

                if (SpecialLinks.ContainsKey(text.Trim()) && SpecialLinks[text.Trim()] == attri)
                {
                    continue;
                }

                var subContent = text.ToLower().Replace("-", " ").Replace("@", " ").Split(" ");
                var flag = false;

                foreach (string s in subContent)
                {
                    if (attri?.ToLower().Replace(".", "").Contains(s) ?? false)
                    {
                        flag = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (!flag)
                {
                    failCount++;
                    failMsg = failMsg + text.Trim() + ": " + attri + "\n";
                }
            }

            ClassicAssert.Zero(failCount, failMsg);
        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public void TestLinkNotDisplayed(string testLink)
        {
            var errorList = new List<string>();
            var web = new HtmlWeb();
            var doc = web.Load(testLink);
            MatchCollection matches = Regex.Matches(doc.DocumentNode.SelectSingleNode("/html").InnerText, @"\[.*\]\[.*[^source]\]");
            foreach (Match match in matches)
            {
                errorList.Add(match.Value);
            }
            ClassicAssert.Zero(errorList.Count, string.Join("\n", errorList));
        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public void TestUnparsedParams(string testLink)
        {
            var errorList = new List<string>();
            var web = new HtmlWeb();
            var doc = web.Load(testLink);
            MatchCollection matches = Regex.Matches(doc.DocumentNode.SelectSingleNode("/html").InnerText, @"\:\S*");
            foreach (Match match in matches)
            {
                if(match.Value != ":" && !match.Value.Contains("://"))
                    errorList.Add(match.Value);
            }
            ClassicAssert.Zero(errorList.Count, string.Join("\n", errorList));
        }
    }
}
