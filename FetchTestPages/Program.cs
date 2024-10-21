using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace FetchTestPages
{
    public class FetchTestPages
    {
        private static readonly string PYTHON_SDK_API_URL_PREFIX = "https://learn.microsoft.com/en-us/python/api";
        private static readonly string PYTHON_SDK_API_URL_SUFFIX = "view=azure-python";
        static void Main(string[] args)
        {
            // Default Configuration
            using IHost host = Host.CreateApplicationBuilder(args).Build();

            IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

            string? service = config["ServiceName"];
            string? package = config["PackageName"];

            // Fetch all need to be validated pages in a service/packages.
            List<string> pages = new List<string>();

            pages.Add(GetServiceHomePage(service));

            pages.Add(GetPackagesHomePage(package));

            string apiRefDocPage = GetApiRefDocPage(package);

            pages.Add(apiRefDocPage);

            List<string> subPages = GetAllSubPages(apiRefDocPage, package);

            pages.AddRange(subPages);

            ExportData(pages);

            host.RunAsync();
        }

        static string GetServiceHomePage(string? serviceName)
        {
            serviceName = serviceName?.ToLower().Replace(" ", "-");
            return $"{PYTHON_SDK_API_URL_PREFIX}/overview/azure/{serviceName}?{PYTHON_SDK_API_URL_SUFFIX}";
        }

        static string GetPackagesHomePage(string? packageName) {
            packageName = packageName?.ToLower().Replace("azure-", "");
            return $"{PYTHON_SDK_API_URL_PREFIX}/overview/azure/{packageName}-readme?{PYTHON_SDK_API_URL_SUFFIX}";
        }

        static string GetApiRefDocPage(string? packageName)
        {
            return $"{PYTHON_SDK_API_URL_PREFIX}/{packageName}/{packageName?.ToLower().Replace("-",".")}?{PYTHON_SDK_API_URL_SUFFIX}";
        }

        static List<string> GetAllSubPages(string apiRefDocPage, string? packageName)
        {
            List<string> subPages = new List<string>();

            List<string> pkgsAndClassesPages = GetPkgsAndClassesPages(apiRefDocPage);

            foreach (var page in pkgsAndClassesPages)
            {
                string pkgAndClassesPage = $"{PYTHON_SDK_API_URL_PREFIX}/{packageName}/" + page;
                subPages.Add(pkgAndClassesPage);
                List<string> subPackagesPages = GetSubPackagesPages(pkgAndClassesPage);

                foreach (var subPackagePage in subPackagesPages) { 
                    subPages.Add($"{PYTHON_SDK_API_URL_PREFIX}/{packageName}/" + subPackagePage);
                }
            }

            return subPages;
        }
        static List<string> GetPkgsAndClassesPages(string apiRefDocPage)
        {
            HtmlWeb web = new HtmlWeb();
            var doc = web.Load(apiRefDocPage);
            var aNodes = doc.DocumentNode.SelectNodes("//td/a");
            return aNodes.Select(pages => pages.Attributes["href"].Value).ToList();
        }

        static List<string> GetSubPackagesPages(string subPage)
        {
            HtmlWeb web = new HtmlWeb();
            var doc = web.Load(subPage);
            List<string> subPackagesPages = new List<string>();
            var h1Node = doc.DocumentNode.SelectSingleNode("//h1").InnerText;
            if (h1Node.Contains("Package")) {
                var aNodes = doc.DocumentNode.SelectNodes("//td/a");
                subPackagesPages.AddRange(aNodes.Select(pages => pages.Attributes["href"].Value).ToList());
            }
            return subPackagesPages;
        }

        static void ExportData(List<string> pages)
        {
            string jsonString = JsonSerializer.Serialize(pages);
            Console.WriteLine(jsonString);
            File.WriteAllText("../../../../DataAutoFramework/appsettings.json", jsonString);
        }
    }
}