using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPTrans.StatusCartaoPersonalizado.Selenium.Extensions
{
    public static class WebElementExtensions
    {
        public static bool IsDisplayed(this IWebElement webElement)
        {
            try
            {
                return webElement.Displayed;
            }
            catch 
            {
                return false;
            }
        }

        public static IWebElement FindElementSafe(this IWebDriver webElement, By by)
        {
            try
            {
                return webElement.FindElement(by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }


        public static bool Exists(this IWebElement element)
        {
            if (element == null)
            { return false; }
            return true;
        }
    }
}
