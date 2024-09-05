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
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-ai-inference/1.0.0b3/azure.ai.inference.aio.html"
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
    }
}
