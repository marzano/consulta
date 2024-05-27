using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using SPTrans.StatusCartaoPersonalizado.Domain.Services;
using SPTrans.StatusCartaoPersonalizado.Selenium.Extensions;
using SPTrans.StatusCartaoPersonalizado.Selenium.Models;
using System;
using System.Threading.Tasks;

namespace SPTrans.StatusCartaoPersonalizado.Selenium.PageObjects
{
    public class SPTransConsultaPage
    {
        protected IWebDriver _driver;

        public IWebElement TextFieldCPF { get { return _driver.FindElement(By.Name("consultaCartaoSearch.usuario.cpfNumber")); } }
        public IWebElement CheckBoxTermo { get { return _driver.FindElement(By.Name("consultaCartaoSearch.termoAceite")); } }
        public IWebElement CaptchaRequest { get { return _driver.FindElementSafe(By.ClassName("g-recaptcha")); } }
        public IWebElement ButtonConsultar { get { return _driver.FindElement(By.Id("formConsultaCartao_0")); } }
        public string SiteKey { get { return CaptchaRequest.GetAttribute("data-sitekey"); } }
        public bool ConsultaEfetuadaComSucesso => _driver.FindElementSafe(By.Id("modalMessage")).Exists() == false;
        public string TextModalMessage { get { return _driver.FindElementSafe(By.CssSelector("#idMessageModal > ul > li > span"))?.Text ?? string.Empty; } }

        public SPTransConsultaPage(IWebDriver webDriver)
        {
            _driver = webDriver;
        }

        public void DigitarCpf(string numeroCpf)
        {
            TextFieldCPF.SendKeys(numeroCpf);
        }

        public void AceitarTermo()
        {
            CheckBoxTermo.Click();
        }

        public void SetCaptchaToken(string twoCaptchaToken)
        {
            var js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript($@"document.getElementById('g-recaptcha-response').innerHTML='{twoCaptchaToken}';");
        }

        public void Consultar()
        {
            ButtonConsultar.Submit();
        }
    }
}
