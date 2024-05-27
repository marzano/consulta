using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SPTrans.StatusCartaoPersonalizado.Selenium.PageObjects
{
    public interface ISPTransDadosCartaoPage
    {
        bool PossuiCartaoPersonalizado();
        bool PossuiCartaoPersonalizadoAtivo();
        string NumeroCartao { get; }
    }

    public class SPTransDadosCartaoPage
    {
        protected IWebDriver _driver;

        public IEnumerable<IWebElement> DivsConteudoCartao { get { return _driver.FindElements(By.ClassName("conteudo-cartao")); } }

        public IEnumerable<IWebElement> CartoesPersonalizados => _driver.FindElements(By.ClassName("conteudo-cartao")).Where(CartaoPersonalizado());

        public string NumeroCartao
        {
            get
            {
                string textoCartao = CartoesPersonalizados.First(CartaoAtivo()).FindElement(By.CssSelector("h3")).Text;
                return new string(textoCartao.Where(char.IsDigit).ToArray());
            }
        }

        public SPTransDadosCartaoPage(IWebDriver webDriver)
        {
            _driver = webDriver;
        }

        public bool PossuiCartaoPersonalizado()
        {
            return DivsConteudoCartao.Any(CartaoPersonalizado());
        }

        private Func<IWebElement, bool> CartaoPersonalizado()
        {
            return (box) =>
            {
                var infosCartaoText = box.FindElement(By.ClassName("infos-cartao")).Text;
                return Regex.Match(infosCartaoText, @"Comum Personalizado\b", RegexOptions.Multiline).Success;
            };
        }

        private Func<IWebElement, bool> CartaoAtivo()
        {
            return (boxCard) => { return Regex.Match(boxCard.Text, @"Ativo\b", RegexOptions.Multiline).Success; };
        }

        public bool PossuiCartaoPersonalizadoAtivo()
        {
            return CartoesPersonalizados.Any(CartaoAtivo());
        }

        internal bool PossuiCartaoPersonalizadoInativo()
        {
            return CartoesPersonalizados.Any(CartaoAtivo()) == false;
        }
    }
}
