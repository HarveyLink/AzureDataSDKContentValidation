using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace DataAutoFramework.Helper
{

    public class PlaywrightHelper
    {
        // 静态方法，返回 Page 对象
        public static async Task<IPage> GetPageAsync()
        {
            // 创建 Playwright 实例
            var playwright = await Playwright.CreateAsync();

            // 启动浏览器实例 (例如使用 Chromium 浏览器)
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            // 创建新页面
            var page = await browser.NewPageAsync();

            return page;
        }


    }
}
