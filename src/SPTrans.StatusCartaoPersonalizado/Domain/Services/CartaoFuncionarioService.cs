using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PublicadorConciliacaoFaturamento.Domain.Services;
using SPTrans.StatusCartaoPersonalizado.Domain.Models.Configuration;
using SPTrans.StatusCartaoPersonalizado.Selenium.DTOs;
using System;
using System.Threading.Tasks;

namespace SPTrans.StatusCartaoPersonalizado.Domain.Services
{
    public interface ICartaoFuncionarioService
    {
        void NotificarResultado(DadosCartaoPersonalizadoDTO dadosCartaoPersonalizado, string system, string consumerApplicationName);
        Task RegistrarResultado(DadosCartaoPersonalizadoDTO dadosCartaoPersonalizado);
    }

    public class CartaoFuncionarioService : ICartaoFuncionarioService
    {
        private readonly IMessagingService _messagingService;
        private readonly ILogger<CartaoFuncionarioService> _logger;
        private readonly Messaging _messaging;
        private readonly ISqlService _sqlService;

        public CartaoFuncionarioService(IMessagingService messagingService, ILogger<CartaoFuncionarioService> logger, IOptions<Messaging> messaging, ISqlService sqlService)
        {
            _messaging = messaging.Value ?? throw new ArgumentException(nameof(messaging));


            _messagingService = messagingService;
            _logger = logger;
            _sqlService = sqlService;
        }

        public void NotificarResultado(DadosCartaoPersonalizadoDTO dadosCartaoPersonalizado, string system, string consumerApplicationName)
        {
            string mensagemResultado = JsonConvert.SerializeObject(dadosCartaoPersonalizado);
            _logger.LogInformation($"NOTIFICANDO RESULTADO {mensagemResultado} PARA O SISTEMA {system} APLICAÇÃO CONSUMIDORA {consumerApplicationName}");

            _messagingService.Queue(_messaging.Publishing.Exchange, $"resultado-status-cartao.{system}.{consumerApplicationName}", mensagemResultado);

            _logger.LogInformation($"NOTIFICADO COM SUCESSO");
        }

        public async Task RegistrarResultado(DadosCartaoPersonalizadoDTO dadosCartaoPersonalizado)
        {
            _logger.LogInformation("REGISTRANDO RESULTADO NA BASE DE DADOS");

            dadosCartaoPersonalizado.DataHoraProcessamento = DateTime.Now;

            var sql = @"INSERT INTO dbo.CARTAOFUNCIONARIO (NumCpf, NumCartao, StatusCartao, Motivo, DataHoraProcessamento, CodigoStatusCartao)
                           OUTPUT INSERTED.[IdCartaoFuncionario] AS IdCartaoFuncionario 
                        VALUES (@CPF, @NumeroCartao, @StatusCartao, @Motivo, @DataHoraProcessamento, @StatusSPTrans)";

            var @params = new
            {
                dadosCartaoPersonalizado.CPF,
                dadosCartaoPersonalizado.NumeroCartao,
                dadosCartaoPersonalizado.StatusCartao,
                dadosCartaoPersonalizado.Motivo,
                dadosCartaoPersonalizado.DataHoraProcessamento,
                dadosCartaoPersonalizado.StatusSPTrans
            };

            dadosCartaoPersonalizado.IdCartaoFuncionario = await _sqlService.GetAsync<int>(sql, @params);

            _logger.LogInformation($"REGISTRADO COM SUCESSO ID {dadosCartaoPersonalizado.IdCartaoFuncionario}");
        }
    }
}
