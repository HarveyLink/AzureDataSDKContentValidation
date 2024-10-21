using DataAutoFramework.Helper;
using HtmlAgilityPack;
using NUnit.Framework.Legacy;
using NUnit.Framework;

namespace DataAutoFramework.TestCases
{
    public class TestPageText
    {
        public static List<string> TestLinks { get; set; }

        static TestPageText()
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
        public void TestBlankNode(string testLink)
        {
            var blankNodeCount = 0;
            var web = new HtmlWeb();
            var doc = web.Load(testLink);
            HtmlNodeCollection items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'admonition seealso')]/ul[contains(@class, 'simple')]/li");
            if(items != null && items.Count > 0)
            {
                foreach (var item in items)
                {
                    blankNodeCount += String.IsNullOrEmpty(item.InnerText) ? 1 : 0;
                }
            }
            
            ClassicAssert.Zero(blankNodeCount);
        }
    }
}
