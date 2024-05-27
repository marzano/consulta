using System;

namespace SPTrans.StatusCartaoPersonalizado.Selenium.DTOs
{
    public class DadosCartaoPersonalizadoDTO
    {
        public int IdCartaoFuncionario { get; set; }
        public string CPF { get; set; }
        public string NumeroCartao { get; set; }
        public string StatusCartao { get; set; }
        public string Motivo { get; set; }
        public DateTime DataHoraProcessamento { get; set; }
        public string CorrelationID { get; internal set; }
        public StatusSPTrans StatusSPTrans { get; set; }
    }

    public enum StatusSPTrans
    {
        UsuarioNaoEncontrado = 1,
        CartaoPersonalizadoInativo = 2,
        CartaoPersonalizadoAtivo = 3,
        UsuarioSemCartaoPersonalizado = 4,
        SituacaoDesconhecida = 5
    }
}
