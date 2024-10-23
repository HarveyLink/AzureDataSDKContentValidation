using NUnit.Framework.Legacy;
using NUnit.Framework;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace DataAutoFramework.TestCases
{
    public class TestPageCodeFormat
    {
        public static List<string> TestLinks { get; set; }

        static TestPageCodeFormat()
        {
            TestLinks = new List<string>
            {
                "https://learn.microsoft.com/en-us/python/api/overview/azure/app-configuration?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/overview/azure/appconfiguration-readme?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration.aio?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration.aio.azureappconfigurationclient?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/azure-appconfiguration/azure.appconfiguration.azureappconfigurationclient?view=azure-python",
                "https://learn.microsoft.com/en-us/python/api/azure-confidentialledger/azure.confidentialledger.aio.confidentialledgerclient"
            };
        }

        [Test]
        [TestCaseSource(nameof(TestLinks))]
        public async Task TestIncorrectFormatJson(string testLink)
        {
            // Error list
            List<string> errorList = new List<string>();

            // Match list
            List<string> matchJsonList = new List<string> {
                "response body for status",
                "JSON input template"
            };

            // Create browser
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(testLink);

            // Get page code
            IReadOnlyList<ILocator> codeHtmlElements = await page.Locator("code.lang-python").AllAsync();

            // Iterate through code elements
            for (int i = 0; i < codeHtmlElements.Count; i++)
            {
                string input = await codeHtmlElements[i].InnerTextAsync();
                if (matchJsonList.Any(input.Contains))
                {
                    // Parse JSON
                    // Call method to remove comments
                    input = RemoveComments(input);

                    // Call method to replace ...
                    input = ReplaceEllipses(input);

                    // Call method to extract content within outer brackets
                    List<string> extractedContent = ExtractOuterBrackets(input);

                    // Output extracted content
                    foreach (var content in extractedContent)
                    {
                        // Call method to validate JSON
                        bool isValidJson = IsValidJson(content);
                        if (!isValidJson)
                        {
                            errorList.Add(input);
                        }
                    }
                }
            }

            await browser.CloseAsync();

            ClassicAssert.Zero(errorList.Count, testLink + " has testLink has improperly displayed code comments in JSON snippet :  " + string.Join(",", errorList));
        }

        // Method to extract content within outer brackets
        List<string> ExtractOuterBrackets(string input)
        {
            List<string> extractedContent = new List<string>();
            Stack<int> bracketStack = new Stack<int>();
            int startIndex = -1;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '{')
                {
                    // If stack is empty, record start position
                    if (bracketStack.Count == 0)
                    {
                        startIndex = i;
                    }
                    // Push current '{' position onto stack
                    bracketStack.Push(i);
                }
                else if (input[i] == '}')
                {
                    // Pop a '{' from the stack
                    bracketStack.Pop();

                    // If stack is empty, a complete outer bracket is found
                    if (bracketStack.Count == 0 && startIndex != -1)
                    {
                        // Extract content within brackets
                        string content = input.Substring(startIndex, i - startIndex + 1);
                        extractedContent.Add(content);
                        startIndex = -1;
                    }
                }
            }

            return extractedContent;
        }

        // Remove comments after each line's #
        string RemoveComments(string input)
        {
            // Use regex to remove content after #
            string pattern = @"\s*#.*";
            return Regex.Replace(input, pattern, string.Empty);
        }

        // Replace matched ... with ""
        string ReplaceEllipses(string input)
        {
            // Define regex pattern
            string pattern = @"\.{3}"; // Match three dots
                                       // Use Regex.Replace method to replace
            return Regex.Replace(input, pattern, "\"\"");
        }

        // Method to validate if JSON is valid
        bool IsValidJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            try
            {
                var parsedJson = JsonDocument.Parse(input);
                return true; // If no exception is thrown, it is valid JSON
            }
            catch (JsonException)
            {
                return false; // If an exception is thrown, it is not valid JSON
            }
        }
    }
}
