using DataAutoFramework.Helper;
using HtmlAgilityPack;
using NUnit.Framework.Legacy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace DataAutoFramework.TestCases
{
    public class TestPageText
    {
        public static List<string> TestLinks { get; set; }

        static TestPageText()
        {
            TestLinks = new List<string>
            {
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mgmt-agrifood/1.0.0b3/index.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mgmt-advisor/9.0.0/azure.mgmt.advisor.operations.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-ai-inference/1.0.0b3/azure.ai.inference.aio.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-ai-inference/1.0.0b3/index.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-ai-generative/1.0.0b8/azure.ai.generative.evaluate.metrics.html"
            };
        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public async Task TestExtraChar(string testLink)
        {
            var errorList = new List<string>();
            var web = new HtmlWeb();
            var doc = web.Load(testLink);
            foreach (var item in doc.DocumentNode.SelectNodes("//p"))
            {
                var text = item.InnerText.Trim();
                if (text.StartsWith("â\u0080\u0099") || text.EndsWith("â\u0080\u0099") || text.StartsWith('~') || text.EndsWith('~'))
                {
                    errorList.Add(text);
                }
            }

            ClassicAssert.Zero(errorList.Count, testLink + " has extra charactor of '-' and `~` at " + string.Join(",", errorList));
        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public async Task TestCodeBlock(string testLink)
        {
            var errorList = new List<string>();
            var web = new HtmlWeb();
            var doc = web.Load(testLink);
            foreach (var item in doc.DocumentNode.SelectNodes("//div[contains(@class, 'notranslate')]"))
            {
                var text = item.InnerText;
                text = text.TrimEnd('\n');
                var newCode = await ValidationHelper.ParsePythonCode(text);
                Console.WriteLine(text);
            }

            ClassicAssert.Zero(errorList.Count, testLink + " has wrong format" + string.Join(",", errorList));
        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public void TestLinkNotDisplayed(string testLink)
        {
            var errorList = new List<string>();
            var web = new HtmlWeb();
            var doc = web.Load(testLink);
            MatchCollection matches = Regex.Matches(doc.DocumentNode.SelectSingleNode("/html").InnerText, @"\[.*\]\[.*[^source]\]");
            foreach(Match match in matches)
            {
                errorList.Add(match.Value);
            }
            ClassicAssert.Zero(errorList.Count, string.Join("\n", errorList));
        }
    }
}
