using HtmlAgilityPack;
using NUnit.Framework.Legacy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Playwright;

namespace DataAutoFramework.TestCases
{
    public class TestPageLabel
    {
        public static List<string> TestLinks { get; set; }

        static TestPageLabel()
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
        public async Task TestExtraLabel(string testLink)
        {
            var errorList = new List<string>();

            var labelList = new List<string> { 
                "<br",
                "<h1",
                "<h2",
                "<h3",
                "<h4",
                "<h5",
                "<h6",
                "<em",
                "<a",
                "<span",
                "<div",
                "<ul",
                "<ol",
                "<li",
                "<table",
                "<tr",
                "<td",
                "<th",
                "<img",
                "<code",
                "&amp;",
                "&lt",
                "&gt",
                "&quot",
                "&apos"
            };


            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();

            await page.GotoAsync(testLink);

            var text = await page.Locator("html").InnerTextAsync();


            foreach (var label in labelList)
            {

                if (text.Contains(label))
                {
                    errorList.Add(label);
                }
            }

            await browser.CloseAsync();

            ClassicAssert.Zero(errorList.Count, testLink + " has extra label of  " + string.Join(",", errorList));
        }
    }
}
