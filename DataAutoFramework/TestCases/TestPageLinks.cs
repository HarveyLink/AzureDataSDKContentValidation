using HtmlAgilityPack;
using NUnit;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAutoFramework.Helper;
using NUnit.Framework.Legacy;
using System.Collections;
using System.Text.RegularExpressions;

namespace DataAutoFramework.TestCases
{
    public class TestPageLinks
    {
        public static List<string> TestLinks { get; set; }

        static TestPageLinks()
        {
            TestLinks = new List<string>
            {
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mgmt-agrifood/1.0.0b3/index.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mgmt-advisor/9.0.0/azure.mgmt.advisor.operations.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-ai-inference/1.0.0b3/azure.ai.inference.aio.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mixedreality-remoterendering/1.0.0b2/azure.mixedreality.remoterendering.html#"
            };
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
        public void TestCrossLinks(string testLink)
        {
            var web = new HtmlWeb();
            var doc = web.Load(testLink);
            var failCount = 0;
            var failMsg = "";

            foreach (HtmlNode aNode in doc.DocumentNode.SelectNodes("//a"))
            {
                string content = aNode.InnerText;
                if (Regex.Replace(aNode.InnerText, @"\s", "") == "" || aNode.GetAttributeValue("title", "") == "Permalink to this headline" || aNode.GetAttributeValue("href", "") == "#")
                {
                    continue;
                }

                string link = aNode.GetAttributeValue("href", "");
                var subContent = content.ToLower().Replace(".", " ").Split(" ");
                var flag = false;

                foreach (string s in subContent)
                {
                    if (link.ToLower().Replace(".", " ").Contains(s))
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
                    failMsg = failMsg + content + ": " + link + "\n";
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
