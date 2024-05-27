namespace SPTrans.StatusCartaoPersonalizado.Domain.Models.DTOs
{
    public class RequisicaoConsultaCartaoDTO
    {
        public string CorrelationId { get; set; }
        public string CPF { get; set; }
        public string System { get; set; }
        public string ConsumerApplicationName { get; set; }
    }
}
