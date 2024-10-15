using NUnit.Framework.Legacy;
using NUnit.Framework;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace DataAutoFramework.TestCases
{
    public class TestPageType
    {
        public static List<string> TestLinks { get; set; }

        static TestPageType()
        {
            TestLinks = new List<string>
            {
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mgmt-agrifood/1.0.0b3/index.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mgmt-advisor/9.0.0/azure.mgmt.advisor.operations.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-ai-inference/1.0.0b3/azure.ai.inference.aio.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-ai-inference/1.0.0b3/index.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-ai-generative/1.0.0b8/azure.ai.generative.evaluate.metrics.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mgmt-advisor/9.0.0/azure.mgmt.advisor.operations.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mgmt-web/7.3.0/azure.mgmt.web.v2023_12_01.operations.html",
                "https://azuresdkdocs.blob.core.windows.net/$web/python/azure-mixedreality-remoterendering/1.0.0b2/azure.mixedreality.remoterendering.html#azure.mixedreality.remoterendering.RenderingSessionSize"
            };

        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public async Task TestExtraLabel(string testLink)
        {
            var errorList = new List<string>();
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(testLink);

            var classList = await page.Locator(".py.class").AllAsync();
            var pyClassParamMap = await GetParamMap(page, ".py.class");
            var pyMethodParamMap = await GetParamMap(page, ".py.method");
            var pyClassErrorList = await ValidParamMap(pyClassParamMap, true);
            var pyMethodErrorList = await ValidParamMap(pyMethodParamMap, false); 

            errorList.AddRange(pyClassErrorList);
            errorList.AddRange(pyMethodErrorList);
            errorList = errorList.Distinct().ToList();

            await browser.CloseAsync();

            ClassicAssert.Zero(errorList.Count, testLink + " has  wrong type annotations of  " + string.Join(",", errorList));
        }

        bool IsCorrectTypeAnnotation(string text)
        {
            if (text == "*")
            {
                return true;
            }
            else if (text == "**kwargs" || text == "*args")
            {
                return false;
            }
            else if (Regex.IsMatch(text, @"^[^=]+=[^=]+$"))
            {
                return true;
            }
            else if (text.Contains(":"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        async Task<Dictionary<string, IReadOnlyList<ILocator>>> GetParamMap(IPage page, string selector)
        {
            Dictionary<string, IReadOnlyList<ILocator>> paramMap = new Dictionary<string, IReadOnlyList<ILocator>>();
            var HTMLElementList = await page.Locator(selector).AllAsync();

            for (int i = 0; i < HTMLElementList.Count; i++)
            {
                var HTMLElement = HTMLElementList[i];
                var dtElement = HTMLElement.Locator("dt").Nth(0);
                var dtId = await dtElement.GetAttributeAsync("id");
                var paramList = await dtElement.Locator(".sig-param").AllAsync();

                paramMap[dtId] = paramList;
            }

            return paramMap;
        }


        async Task<List<string>> ValidParamMap(Dictionary<string, IReadOnlyList<ILocator>> paramMap,bool isClass) {

            var errorList = new List<string>();

            foreach (var item in paramMap)
            {
                string dtId = item.Key;
                var paramList = item.Value;

                if (isClass && paramList.Count == 0)
                {
                    errorList.Add($"Empty argument : {dtId}");
                    Console.WriteLine($"Empty argument : {dtId}");
                }

                for (int i = 0; i < paramList.Count; i++)
                {
                    var text = await paramList[i].InnerTextAsync();

                    if (!IsCorrectTypeAnnotation(text))
                    {
                        errorList.Add($"Missing type annotations : {dtId}");
                        Console.WriteLine($"type argument : {text}");
                    }
                }
            }

            return errorList;
        }

    }
}
