using AutoFixture;
using Microsoft.Extensions.Options;
using SPTrans.StatusCartaoPersonalizado.Configurations.Factories;
using SPTrans.StatusCartaoPersonalizado.Domain.Services;
using SPTrans.StatusCartaoPersonalizado.Selenium.Models;
using SPTrans.StatusCartaoPersonalizado.Selenium.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SPTrans.StatusCartaoPersonalizado.Test.SeleniumTasks
{
    [Collection("AutoMoqCollection")]
    public class CartaoStatusSPTransTaskTest
    {
        private readonly ICartaoStatusSPTransTask _sut;
        private readonly IFixture _fixture;

        public CartaoStatusSPTransTaskTest(AutoMoqFixture autoMoqFixture)
        {
            _fixture = autoMoqFixture.Fixture;

            var seleniumSettings = _fixture.Build<SeleniumSettings>()
                .With(x => x.SPTrans, new Selenium.Models.SPTrans { URL = "https://scapub.sbe.sptrans.com.br/sa/consultaCartao/" })
                .With(x => x.TwoCaptcha, new Selenium.Models.TwoCaptcha { Token = "44b7d7607ce3877d0624dccdaa311e5c" })
                .Create();

            var _seleniumSettingsMock = _fixture.InjectMoq<IOptions<SeleniumSettings>>();
            _seleniumSettingsMock.SetupGet(x => x.Value).Returns(seleniumSettings);

            var webDriver = WebDriverFactory.GetWebDriver();
            _fixture.Inject(webDriver);

            _fixture.Inject<ITwoCaptchaService>(_fixture.Create<TwoCaptchaService>());

            _sut = _fixture.Create<CartaoStatusSPTransTask>();
        }

        public static IEnumerable<object[]> NumeroDocumentoParameters()
        {
            yield return new object[]
            {
                //Esse CPF faz o site da SPTRANS exibir um erro 500, totalmente atípico, deve ser tratado como usuário não encontrado.
                 "29332927855", 
                  CartaoStatusSPTransTask.STATUS_USUARIO_NAO_ENCONTRADO
            };

            yield return new object[]
            {
                 "132546489",
                  CartaoStatusSPTransTask.STATUS_USUARIO_NAO_ENCONTRADO
            };

            yield return new object[]
            {
                 "25249408885",
                 CartaoStatusSPTransTask.STATUS_USUARIO_CADASTRADO_COM_CARTAO_ATIVO
            };
        }

        [MemberData(nameof(NumeroDocumentoParameters))]
        [Theory(DisplayName = "Deve consultar cartão e encontrar status atual para cpf informado")]
        public async Task ConsultarCartao(string numeroCpf, string expectedResult)
        {
            var result = await _sut.ConsultarStatusCartaoPersonalizado(numeroCpf);

            Assert.Equal(result.StatusCartao, expectedResult);
        }
    }
}
