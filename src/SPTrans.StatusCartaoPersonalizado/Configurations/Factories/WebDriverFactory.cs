using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;

namespace SPTrans.StatusCartaoPersonalizado.Configurations.Factories
{
    public static class WebDriverFactory
    {
        public static IWebDriver GetWebDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArguments("disable-infobars");
            options.AddArguments("--disable-extensions");
            options.AddArguments("--disable-dev-shm-usage");
            options.AddArguments("--no-sandbox");

#if DEBUG
            //Utilizar somente para DEBUG quando precisar direcionar o executavel da versão correta para o chrome driver
            //options.BinaryLocation = @"C:\Program Files (x86)\Google\Chrome\chrome-win\chrome.exe";
#endif

            IWebDriver chromeDriver = new ChromeDriver(options);
            chromeDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);

            return chromeDriver;
        }
    }
}
