using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices.JavaScript;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;

namespace DataAutoFramework.Helper
{
    public class ValidationHelper
    {
        public static async Task<bool> CheckIfPageExist(string url)
        {
            var web = new HtmlWeb();
            var doc = web.Load(url);
            if (web.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }
            return true;
        }

        public static async Task<string> ParsePythonCode(string code)
        {
            var requestUrl = "https://formatter.org/admin/python-format";
            code = code.Replace("&quot;", "\"").Replace("\n", "\r\n");
            code = HttpUtility.UrlEncode(code, Encoding.UTF8);
            StringBuilder payload = new StringBuilder();
            payload.Append("\"codeSrc\":\"").Append(code).Append("\",\"skip_string\":false,\"columnLimit\":\"80\"");
            using var client = new HttpClient();
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var result = await client.PostAsync(requestUrl, content);
            var newCode = await result.Content.ReadFromJsonAsync<CodeBlock>();
            return newCode.CodeDst;
        }
    }

    public class CodeBlock
    {
        public int Errcode { get; set; }
        public string? Errmsg { get; set; }
        public string? CodeDst { get; set; }
    }
}
