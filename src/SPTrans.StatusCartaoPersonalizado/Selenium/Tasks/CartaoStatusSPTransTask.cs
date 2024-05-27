using Microsoft.Extensions.Options;
using SPTrans.StatusCartaoPersonalizado.Domain.Services;
using OpenQA.Selenium;
using SPTrans.StatusCartaoPersonalizado.Selenium.DTOs;
using SPTrans.StatusCartaoPersonalizado.Selenium.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SPTrans.StatusCartaoPersonalizado.Selenium.PageObjects;
using TwoCaptcha.Exceptions;
using Microsoft.ApplicationInsights;
using SPTrans.StatusCartaoPersonalizado.Domain.Models.Configuration;
using System.Collections.Generic;
using SPTrans.StatusCartaoPersonalizado.Configurations.Factories;

namespace SPTrans.StatusCartaoPersonalizado.Selenium.Tasks
{
    public interface ICartaoStatusSPTransTask
    {
        Task<DadosCartaoPersonalizadoDTO> ConsultarStatusCartaoPersonalizado(string numeroCpf);
    }

    public class CartaoStatusSPTransTask : ICartaoStatusSPTransTask
    {
        private IWebDriver _driver;
        private readonly SeleniumSettings _seleniumSettings;
        private readonly ILogger<CartaoStatusSPTransTask> _logger;
        private readonly ITwoCaptchaService _twoCaptchaService;
        private readonly ServiceData _serviceData;
        private readonly TelemetryClient _telemetryClient;

        public const string STATUS_USUARIO_NAO_ENCONTRADO = "CARTÃO/USUÁRIO NÃO ENCONTRADO";
        public const string STATUS_USUARIO_CADASTRADO_COM_CARTAO_INATIVO = "USUÁRIO CADASTRADO E CARTÃO PERSONALIZADO INATIVO";
        public const string STATUS_USUARIO_CADASTRADO_COM_CARTAO_ATIVO = "USUÁRIO CADASTRADO E CARTÃO PERSONALIZADO ATIVO";
        public const string STATUS_USUARIO_SEM_CARTAO_PERSONALIZADO = "USUÁRIO NÃO POSSUI CARTÃO PERSONALIZADO";

        public CartaoStatusSPTransTask(IOptions<SeleniumSettings> seleniumSettings, 
            IWebDriver webDriver, 
            ILogger<CartaoStatusSPTransTask> logger, 
            ITwoCaptchaService twoCaptchaService,
            IOptions<ServiceData> serviceData,
            TelemetryClient telemetryClient)
        {
            _seleniumSettings = seleniumSettings.Value ?? throw new ArgumentException(nameof(seleniumSettings));

            _driver = webDriver;
            _logger = logger;
            _twoCaptchaService = twoCaptchaService;
            _serviceData = serviceData.Value ?? throw new ArgumentNullException(nameof(serviceData));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        public async Task<DadosCartaoPersonalizadoDTO> ConsultarStatusCartaoPersonalizado(string numeroCpf)
        {
            try
            {
                var dadosCartao = new DadosCartaoPersonalizadoDTO()
                {
                    CPF = numeroCpf,
                    DataHoraProcessamento = DateTime.Now
                };

                _logger.LogDebug($"TASK | Navegando até a URL: {_seleniumSettings.SPTrans.URL}");
                _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} TASK | Navegando até a URL: {_seleniumSettings.SPTrans.URL}");
                _driver.Navigate().GoToUrl(_seleniumSettings.SPTrans.URL);

                var consultaPage = new SPTransConsultaPage(_driver);

                _logger.LogDebug($"TASK | DIGITANDO CPF {numeroCpf}");
                consultaPage.DigitarCpf(numeroCpf);

                _logger.LogDebug($"TASK | ACEITANDO TERMO");
                consultaPage.AceitarTermo();

                if (consultaPage.CaptchaRequest != null)
                {
                    _logger.LogDebug($"TASK | RESOLVENDO CAPTCHA");
                    consultaPage.SetCaptchaToken(await ResolverCaptcha(consultaPage.SiteKey));
                }

                _logger.LogDebug($"TASK | EFETUANDO CONSULTA");
                consultaPage.Consultar();


                var dadosCartaoPage = new SPTransDadosCartaoPage(_driver);

                if (consultaPage.ConsultaEfetuadaComSucesso == false)
                {
                    _logger.LogWarning($"TASK | NÃO FOI POSSÍVEL CONSULTAR O CADASTRO DO CPF {numeroCpf}");

                    _logger.LogWarning($"TASK | MODAL MESSAGE: {consultaPage.TextModalMessage}");
                    _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} TASK | NÃO FOI POSSÍVEL CONSULTAR O CADASTRO DO CPF {numeroCpf}");
                    dadosCartao.StatusCartao = STATUS_USUARIO_NAO_ENCONTRADO;
                    dadosCartao.Motivo = consultaPage.TextModalMessage;
                    dadosCartao.StatusSPTrans = StatusSPTrans.UsuarioNaoEncontrado;
                }
                else if (dadosCartaoPage.PossuiCartaoPersonalizado() == false)
                {
                    _logger.LogInformation($"TASK | CARTÃO PERSONALIZADO NÃO IDENTIFICADO PARA O CPF {numeroCpf}");
                    _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} TASK | CARTÃO PERSONALIZADO NÃO IDENTIFICADO PARA O CPF {numeroCpf}");
                    dadosCartao.StatusCartao = STATUS_USUARIO_SEM_CARTAO_PERSONALIZADO;
                    dadosCartao.Motivo = STATUS_USUARIO_SEM_CARTAO_PERSONALIZADO;
                    dadosCartao.StatusSPTrans = StatusSPTrans.UsuarioSemCartaoPersonalizado;
                }
                else if (dadosCartaoPage.PossuiCartaoPersonalizadoAtivo())
                {
                    _logger.LogInformation($"TASK | CARTÃO PERSONALIZADO E ATIVO IDENTIFICADO PARA O CPF {numeroCpf}");
                    _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} TASK | CARTÃO PERSONALIZADO E ATIVO IDENTIFICADO PARA O CPF {numeroCpf}");
                    dadosCartao.StatusCartao = STATUS_USUARIO_CADASTRADO_COM_CARTAO_ATIVO;
                    dadosCartao.NumeroCartao = dadosCartaoPage.NumeroCartao;
                    dadosCartao.Motivo = "Funcionário já possui o Bilhete Único ativo na SPTRANS";
                    dadosCartao.StatusSPTrans = StatusSPTrans.CartaoPersonalizadoAtivo;
                }
                else if (dadosCartaoPage.PossuiCartaoPersonalizadoInativo())
                {
                    _logger.LogInformation($"TASK | NÃO FOI IDENTIFICADO CARTÃO PERSONALIZADO ATIVO PARA O CPF {numeroCpf}");
                    _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} TASK | NÃO FOI IDENTIFICADO CARTÃO PERSONALIZADO ATIVO PARA O CPF {numeroCpf}");
                    dadosCartao.StatusCartao = STATUS_USUARIO_CADASTRADO_COM_CARTAO_INATIVO;
                    dadosCartao.Motivo = "Usuário precisa entrar em contato na SPTRANS";
                    dadosCartao.StatusSPTrans = StatusSPTrans.CartaoPersonalizadoInativo;
                }
                else
                {
                    _logger.LogWarning($"TASK | NÃO FOI POSSÍVEL IDENTIFICAR O STATUS DO CADASTRO E DO CARTÃO PERSONALIZADO DO USUÁRIO");
                    _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} TASK | NÃO FOI POSSÍVEL IDENTIFICAR O STATUS DO CADASTRO E DO CARTÃO PERSONALIZADO DO USUÁRIO");
                    dadosCartao.StatusCartao = "NÃO FOI POSSÍVEL IDENTIFICAR O STATUS DO CADASTRO E DO CARTÃO PERSONALIZADO DO USUÁRIO";
                    dadosCartao.StatusSPTrans = StatusSPTrans.SituacaoDesconhecida;
                }

                _logger.LogInformation($"TASK | PROCESSAMENTO DO CPF {numeroCpf} FINALIZADO");
                _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} TASK | PROCESSAMENTO DO CPF {numeroCpf} FINALIZADO");

                return dadosCartao;
            }
            catch (ApiException ex)
            {
                _logger.LogError($"SPTRANS TASK | 2Captcha API EXCEPTION: {ex}");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>() { { $"{_serviceData.CurrentAppName}-{_serviceData.Environment} SPTRANS TASK | Captcha API EXCEPTION", "" } });
                if (ex.Message.ToUpper() == "ERROR_NO_SLOT_AVAILABLE")
                    await Task.Delay(5000);

                throw;

            }
            catch (NoSuchWindowException ex)
            {
                _logger.LogError(ex, "SELENIUM ERROR | Janela do navegador não foi encontrada");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>() { { $"{_serviceData.CurrentAppName}-{_serviceData.Environment} SELENIUM ERROR | NoSuchWindowException Janela do navegador não foi encontrada", "" } });
                throw;
            }
            catch (NoSuchElementException ex)
            {
                _logger.LogError(ex, "SELENIUM ERROR | Algum elemento da página não foi encontrado");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>() { { $"{_serviceData.CurrentAppName}-{_serviceData.Environment} SELENIUM ERROR | NoSuchElementException Algum elemento da página não foi encontrado", "" } });
                throw;
            }
            catch (Exception ex)
            {
                if(ex.Message.Contains("invalid session id"))
                {
                    _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} WEBDRIVER | SESSAO INVALIDA.");
                    try
                    {
                        _driver.Quit();
                    } catch { }
                    _driver = WebDriverFactory.GetWebDriver();
                    _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} WEBDRIVER | SESSAO RECRIADA.");
                }
                _logger.LogError(ex, "SPTRANS TASK | THE HOUSE IS KABUM ... ");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>() { { $"{_serviceData.CurrentAppName}-{_serviceData.Environment} SPTRANS TASK | EXCEPTION GERAL", "" } });
                throw;
            }
        }

        public async Task<string> ResolverCaptcha(string siteKey)
        {
            return await _twoCaptchaService.SolveReCaptchaV2(siteKey, _seleniumSettings.SPTrans.URL);
        }
    }

}
