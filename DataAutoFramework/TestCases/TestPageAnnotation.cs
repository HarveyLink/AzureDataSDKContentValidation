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
                "https://learn.microsoft.com/en-us/python/api/overview/azure/app-configuration?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/overview/azure/appconfiguration-readme?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration.aio?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration.aio.azureappconfigurationclient?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration.azureappconfigurationclient?view=azure-python"
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

            Dictionary<string, List<string>>? pyClassParamMap = null;
            Dictionary<string, List<string>>? pyMethodParamMap = null;

            if (testLink.Contains("azuresdkdocs", StringComparison.OrdinalIgnoreCase))
            {
                pyClassParamMap = await GetParamMap4AzureSdkDocs(page, true);
                pyMethodParamMap = await GetParamMap4AzureSdkDocs(page, false);
            }
            else if (testLink.Contains("learn.microsoft", StringComparison.OrdinalIgnoreCase))
            {
                pyClassParamMap = await GetParamMap4LearnMicrosoft(page, true);
                pyMethodParamMap = await GetParamMap4LearnMicrosoft(page, false);
            }

            List<string> pyClassErrorList = ValidParamMap(pyClassParamMap!, true);
            List<string> pyMethodErrorList = ValidParamMap(pyMethodParamMap!, false);
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

        async Task<Dictionary<string, List<string>>> GetParamMap4AzureSdkDocs(IPage page, bool isClass)
        {
            Dictionary<string, List<string>> paramMap = new Dictionary<string, List<string>>();

            IReadOnlyList<ILocator>? HTMLElementList = null;
            if (isClass)
            {
                HTMLElementList = await page.Locator(".py.class").AllAsync();
            }
            else
            {
                HTMLElementList = await page.Locator(".py.method").AllAsync();
            }


            for (int i = 0; i < HTMLElementList.Count; i++)
            {
                var HTMLElement = HTMLElementList[i];
                var dtElement = HTMLElement.Locator("dt").Nth(0);
                var key = await dtElement.GetAttributeAsync("id");
                var paramLocatorsList = await dtElement.Locator(".sig-param").AllAsync();

                List<string> paramList = new List<string>();

                foreach (var locator in paramLocatorsList)
                {
                    var innerText = await locator.InnerTextAsync();
                    paramList.Add(innerText);
                }

                paramMap[key] = paramList;
            }

            return paramMap;
        }

        async Task<Dictionary<string, List<string>>> GetParamMap4LearnMicrosoft(IPage page, bool isClass)
        {
            Dictionary<string, List<string>> paramMap = new Dictionary<string, List<string>>();

            IReadOnlyList<ILocator>? HTMLElementList = null;
            if (isClass)
            {
                HTMLElementList = await page.Locator(".content > .wrap.has-inner-focus").AllAsync();
            }
            else
            {
                HTMLElementList = await page.Locator(".memberInfo > .wrap.has-inner-focus").AllAsync();
            }

            for (int i = 0; i < HTMLElementList.Count; i++)
            {

                var HTMLElement = HTMLElementList[i];
                var codeText = await HTMLElement.InnerTextAsync();

                var regex = new Regex(@"(?<key>.+?)\((?<params>.+?)\)");
                var match = regex.Match(codeText);

                if (!match.Success)
                {
                    Console.WriteLine("Ignore codeText : ");
                    Console.WriteLine(codeText);
                    Console.WriteLine("");
                    continue;
                }

                string key = match.Groups["key"].Value.Trim();
                string paramsText = match.Groups["params"].Value.Trim();

                var paramList = SplitParameters(paramsText);

                paramMap[key] = paramList;
            }

            return paramMap;
        }



        List<string> SplitParameters(string paramsText)
        {
            var paramList = new List<string>();
            int bracketCount = 0;
            string currentParam = "";

            for (int i = 0; i < paramsText.Length; i++)
            {
                char c = paramsText[i];

                if (c == '[')
                {
                    bracketCount++;
                }
                else if (c == ']')
                {
                    bracketCount--;
                }
                else if (c == ',' && bracketCount == 0)
                {
                    paramList.Add(currentParam.Trim());
                    currentParam = "";
                    continue;
                }

                currentParam += c;
            }

            if (!string.IsNullOrWhiteSpace(currentParam))
            {
                paramList.Add(currentParam.Trim());
            }

            return paramList;
        }


        List<string> ValidParamMap(Dictionary<string, List<string>> paramMap, bool isClass)
        {

            var errorList = new List<string>();

            foreach (var item in paramMap)
            {
                string key = item.Key;
                var paramList = item.Value;

                if (isClass && paramList.Count == 0)
                {
                    errorList.Add($"Empty argument : {key}");
                    Console.WriteLine($"Empty argument : {key}");
                }

                for (int i = 0; i < paramList.Count; i++)
                {
                    var text = paramList[i];

                    if (!IsCorrectTypeAnnotation(text))
                    {
                        errorList.Add($"Missing type annotations : {key}");
                        Console.WriteLine($"type argument : {text}");
                    }
                }
            }

            return errorList;
        }

    }
}
