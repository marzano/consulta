using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenQA.Selenium;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SPTrans.StatusCartaoPersonalizado.Configurations.Factories;
using SPTrans.StatusCartaoPersonalizado.Domain.Models.Configuration;
using SPTrans.StatusCartaoPersonalizado.Domain.Models.DTOs;
using SPTrans.StatusCartaoPersonalizado.Domain.Services;
using SPTrans.StatusCartaoPersonalizado.Selenium.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SPTrans.StatusCartaoPersonalizado
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ICartaoStatusSPTransTask _taskNavigator;
        private readonly IMessagingService _messagingService;
        private readonly IMessagingFactory _messagingFactory;
        private readonly IDatabaseFactory _databaseFactory;
        private readonly ICartaoFuncionarioService _cartaoFuncionarioService;
        private readonly IWebDriver _webDriver;

        private readonly Messaging _messaging;
        private readonly ServiceData _serviceData;
        private readonly TelemetryClient _telemetryClient;

        public Worker(ILogger<Worker> logger, 
            ICartaoStatusSPTransTask taskNavigator, 
            IMessagingService messagingService, 
            IMessagingFactory messagingFactory, 
            IDatabaseFactory databaseFactory, 
            IOptions<Messaging> messaging, 
            ICartaoFuncionarioService cartaoFuncionarioService,
            IWebDriver webDriver,
            IOptions<ServiceData> serviceData,
            TelemetryClient telemetryClient)
        {
            _messaging = messaging.Value ?? throw new ArgumentException(nameof(messaging));

            _logger = logger;
            _taskNavigator = taskNavigator;
            _messagingService = messagingService;
            _messagingFactory = messagingFactory;
            _databaseFactory = databaseFactory;
            _cartaoFuncionarioService = cartaoFuncionarioService;
            _webDriver = webDriver;
            _serviceData = serviceData.Value ?? throw new ArgumentNullException(nameof(serviceData));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (_telemetryClient.StartOperation<RequestTelemetry>("consulta_sptrans-status-cartao"))
            {
                _telemetryClient.Context.InstrumentationKey = _serviceData.InstrumentationKey;
                _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} INICIANDO PROCESSAMENTO");
                _telemetryClient.Flush();
            }

            var channel = _messagingFactory.ConfigureConsumer();
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += _messagingService.AsyncDequeue(stoppingToken, async (string raw) =>
            {
                using (_telemetryClient.StartOperation<RequestTelemetry>("consulta_sptrans-status-cartao"))
                {
                    _telemetryClient.Context.InstrumentationKey = _serviceData.InstrumentationKey;
                    _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} MSG RECEBIDA");
                    stoppingToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        return;
                    }

                    using (_logger.BeginScope(Guid.NewGuid().ToString()))
                    {
                        try
                        {
                            await _databaseFactory.OpenConnectionAsync();

                            _databaseFactory.BeginTransaction();

                            var requisicaoConsulta = JsonConvert.DeserializeObject<RequisicaoConsultaCartaoDTO>(raw);

                            var resultadoCartao = await _taskNavigator.ConsultarStatusCartaoPersonalizado(requisicaoConsulta.CPF);

                            await _cartaoFuncionarioService.RegistrarResultado(resultadoCartao);

                            resultadoCartao.CorrelationID = requisicaoConsulta.CorrelationId;

                            _cartaoFuncionarioService.NotificarResultado(resultadoCartao, requisicaoConsulta.System, requisicaoConsulta.ConsumerApplicationName);

                            _logger.LogInformation("WORKER | EVENTO PROCESSADO COM SUCESSO");
                            _telemetryClient.TrackEvent($"{_serviceData.CurrentAppName}-{_serviceData.Environment} WORKER | EVENTO PROCESSADO COM SUCESSO");
                            _telemetryClient.Flush();

                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical($"WORKER | THE HOUSE IS KABUM: {ex}");
                            _telemetryClient.TrackException(ex, new Dictionary<string, string>() { { $"{_serviceData.CurrentAppName}-{_serviceData.Environment} WORKER EXCEPTION", "" } });
                            _telemetryClient.Flush();
                            throw;
                        }
                        finally
                        {
                            _databaseFactory.CommitTransaction();
                            _databaseFactory.CloseConnection();
                        }
                    }
                }
            });

            channel.BasicConsume(_messaging.Consuming.Queue, false, Environment.MachineName, consumer);
            

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _webDriver.Quit();
            base.StopAsync(cancellationToken);
            return Task.CompletedTask;
        }
    }
}
